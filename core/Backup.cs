using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ametrin.Serialization;
using IBS.Core.Serialization;

namespace IBS.Core;

public sealed class Backup(DirectoryInfo root, DirectoryInfo storage, FileInfo metaDataFile, FileInfo deletedTimeStampsFile, BackupMetaData metaData, Dictionary<string, DateTime> deletedTimeStamps)
{
    public const string DELETED_EXTENSION = ".deleted";

    public DirectoryInfo Root { get; } = root;
    public DirectoryInfo Storage { get; } = storage;
    public FileInfo MetaDataFile { get; } = metaDataFile;
    public FileInfo DeletedTimeStampsFile { get; } = deletedTimeStampsFile;
    public BackupMetaData MetaData { get; private set; } = metaData;
    public Dictionary<string, DateTime> DeletedTimeStamps { get; private set; } = deletedTimeStamps;

    public void SoftDelete(FileInfo file)
    {
        if (!BelongsHere(file)) throw new ArgumentException("Cannot delete files outside of the current backup", nameof(file));

        var newPath = $"{file.FullName}{DELETED_EXTENSION}";
        file.MoveTo(newPath);
        DeletedTimeStamps[newPath] = DateTime.Now;
    }

    public bool IsSoftDeleted(FileInfo file)
    {
        if (!BelongsHere(file)) throw new ArgumentException("Cannot process files outside of the current backup", nameof(file));

        if (file.Extension is DELETED_EXTENSION && !DeletedTimeStamps.ContainsKey(file.FullName))
        {
            DeletedTimeStamps[file.FullName] = DateTime.Now;
        }

        return file.Extension is DELETED_EXTENSION;
    }

    public bool BelongsHere(FileInfo file) => file.FullName.StartsWith(Storage.FullName, StringComparison.OrdinalIgnoreCase);

    public void Save()
    {
        JsonSerializer.SerializeToFile(MetaDataFile, MetaData, BackupJsonContext.Default.BackupMetaData);
        JsonSerializer.SerializeToFile(DeletedTimeStampsFile, DeletedTimeStamps, BackupJsonContext.Default.DictionaryStringDateTime);
    }

    public static Backup Create(DirectoryInfo root)
    {
        var storage = root.Directory("_Storage");
        var metaDataFile = root.File("metadata.json");
        var deletedTimestampsFile = root.File("deleted.json");

        var metaData = metaDataFile.Exists ? JsonSerializer.Deserialize(metaDataFile, BackupJsonContext.Default.BackupMetaData) : new() { Version = 1 };
        var deletedTimestamps = deletedTimestampsFile.Exists ? JsonSerializer.Deserialize(deletedTimestampsFile, BackupJsonContext.Default.DictionaryStringDateTime).ToDictionary(StringComparer.OrdinalIgnoreCase) : new(StringComparer.OrdinalIgnoreCase);

        return new Backup(root, storage, metaDataFile, deletedTimestampsFile, metaData, deletedTimestamps);
    }
}

public sealed class BackupMetaData
{
    public int Version { get; set; } = 1;
    public DateTime LastWriteTime { get; set; } = DateTime.MinValue;
}

public sealed class BackupV2(DirectoryInfo root, DirectoryInfo storage, FileInfo metaDataFile, FileInfo treeFile, BackupMetaData metaData, JsonNode rootNode)
{
    public DirectoryInfo Root { get; } = root;
    public DirectoryInfo Storage { get; } = storage;
    public FileInfo TreeFile { get; } = treeFile;
    public FileInfo MetaDataFile { get; } = metaDataFile;
    public BackupMetaData MetaData { get; private set; } = metaData;
    private readonly JsonNode rootNode = rootNode;

    public async Task Backup(string path, DirectoryInfo origin)
    {
        DirectoryNotFoundException.ExistsOrThrow(origin);

        var source = origin.File(path);
        FileNotFoundException.ExistsOrThrow(source);

        using var stream = source.OpenRead();
        var hash = new string(Base64Url.EncodeToChars(stream.ComputeSHA256Hash()));
        var destination = GetFileInfoFromHash(hash, source.Extension);

        var node = GetOrCreateNode(path);
        var info = node.Deserialize<NodeInfo>() ?? throw new JsonException();
        if (info.GetLatest()?.Hash == hash)
        {
            Debug.Assert(destination.Exists);
            Debug.Assert(source.LastWriteTimeUtc == destination.LastWriteTimeUtc);
            Debug.Assert(source.Length == destination.Length);
        }
        else
        {
            info.Versions.Add(new(DateTime.Now, hash));
            info.DeletedAt = null;
            node.ReplaceWith(info);
        }

        if (!destination.Exists)
        {
            await source.CopyToAsync(destination);
        }
    }

    public ErrorState SoftDelete(FileInfo file)
    {
        if (!BelongsHere(file)) throw new ArgumentException("Cannot process files outside of the current backup", nameof(file));

        var path = file.GetRelativePath(Storage);
        if (!GetNode(path).Branch(out var node))
        {
            return new FileNotFoundException(null, file.FullName);
        }

        return SoftDelete(node);
    }

    public ErrorState SoftDelete(JsonNode node)
    {
        var info = node.Deserialize<NodeInfo>()!;

        if (info.DeletedAt is DateTime) return new InvalidOperationException("Cannot delete an already deleted file");

        info.DeletedAt = DateTime.Now;
        var newest = info.Versions.MaxBy(static v => v.SavedAt)!;
        info.Versions.Clear();
        info.Versions.Add(newest);

        node.ReplaceWith(info);

        return default;
    }

    public bool IsSoftDeleted(FileInfo file)
    {
        if (!BelongsHere(file)) throw new ArgumentException("Cannot process files outside of the current backup", nameof(file));

        var path = file.GetRelativePath(Storage);
        return IsSoftDeleted(path);
    }

    public bool IsSoftDeleted(string path)
    {
        return GetNode(path).Map(IsSoftDeleted).Or(false);
    }

    public bool IsSoftDeleted(JsonNode node)
    {
        Debug.Assert(node[nameof(NodeInfo.Version)] is not null);
        return node[nameof(NodeInfo.DeletedAt)] is not null;
    }

    public bool BelongsHere(FileInfo file) => file.FullName.StartsWith(Storage.FullName, StringComparison.OrdinalIgnoreCase);

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await JsonSerializer.SerializeToFileAsync(MetaDataFile, MetaData, BackupJsonContext.Default.BackupMetaData, overwrite: true, cancellationToken);
        await JsonSerializer.SerializeToFileAsync(TreeFile, rootNode, options: null, overwrite: true, cancellationToken);
    }

    internal IEnumerable<FileNode> GetFiles(string path)
    {
        return GetNode(path)
              .Require(static node => node[nameof(NodeInfo.Versions)] is null) // require current node to be a directory
              .Map(node => node.AsObject()
                            .Where(static child => child.Value?[nameof(NodeInfo.Versions)] is not null) // filter for files
                            .Select(child => FileNode.Create(this, path, child.Key, child.Value!))
                  )
              .Or([]);
    }

    internal IEnumerable<(string name, JsonNode node)> GetDirectories(string path)
    {
        return GetNode(path)
              .Require(static node => node[nameof(NodeInfo.Versions)] is null) // require current node to be a directory
              .Map(node => node.AsObject()
                            .Where(static child => child.Value?[nameof(NodeInfo.Versions)] is null) // filter for directories
                            .Select(static pair => (pair.Key, pair.Value!))
                  )
              .Or([]);
    }

    private static readonly SearchValues<char> PathSplitChars = SearchValues.Create([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
    public Option<JsonNode> GetNode(ReadOnlySpan<char> path)
    {
        using var nodeNameRanges = path.SplitAny(PathSplitChars);
        var node = rootNode;
        foreach (var range in nodeNameRanges)
        {
            if (!node.AsObject().TryGetPropertyValue(path[range].ToString(), out node!))
            {
                return default;
            }
        }
        return node;
    }

    private JsonNode GetOrCreateNode(ReadOnlySpan<char> path)
    {
        using var nodeNameRanges = path.SplitAny(PathSplitChars);
        var node = rootNode;
        foreach (var range in nodeNameRanges)
        {
            var name = path[range].ToString();
            if (node[name] is not { } child)
            {
                child = new JsonObject();
                node[name] = child;
            }
            node = child!;
        }
        return node;
    }

    private FileInfo GetFileInfoFromHash(string hash, string extension)
    {
        var folderName = $"d-{hash.AsSpan(..6)}";
        var fileName = $"f-{hash}{extension}";
        return Storage.Directory(folderName).File(fileName);
    }

    public static async Task<BackupV2> CreateAsync(DirectoryInfo root)
    {
        var storage = root.Directory("_Storage");
        var metaDataFile = root.File("metadata.json");
        var treeFile = root.File("tree.json");

        var metaData = metaDataFile.Exists ? await JsonSerializer.DeserializeAsync(metaDataFile, BackupJsonContext.Default.BackupMetaData) : new() { Version = 2 };
        var rootNode = treeFile.Exists ? await JsonNode.ParseAsync(treeFile) : new JsonObject();

        return new BackupV2(root, storage, metaDataFile, treeFile, metaData, rootNode);
    }

    internal sealed class FileNode
    {
        public BackupV2 Backup { get; }
        public string Name { get; }
        public string Path { get; }
        public JsonNode Node { get; private set; }

        private FileNode(BackupV2 backup, string name, string path, JsonNode node)
        {
            Backup = backup;
            Name = name;
            Path = path;
            Node = node;
        }

        // public FileInfo GetFileInfo() => Backup.GetFileInfoFromHash(Info.GetLatest().Hash);

        public void SoftDelete()
        {
            Backup.SoftDelete(Node);
        }

        public static FileNode Create(BackupV2 backup, string path) => new(backup, System.IO.Path.GetDirectoryName(path)!, path, backup.GetNode(path).OrThrow());
        public static FileNode Create(BackupV2 backup, string parentPath, string name)
        {
            var path = System.IO.Path.Join(parentPath, name);
            return Create(backup, name, path, backup.GetNode(path).OrThrow());
        }

        public static FileNode Create(BackupV2 backup, string parentPath, string name, JsonNode node)
        {
            Debug.Assert(node.GetPropertyName() == name);
            var path = System.IO.Path.Join(parentPath, name);
            return new(backup, name, path, node);
        }
    }

    internal sealed class NodeInfo(List<NodeInfo.Version> versions, DateTime? deletedAt)
    {
        public List<Version> Versions { get; set; } = versions ?? [];
        public DateTime? DeletedAt { get; set; } = deletedAt;
        public Version? GetLatest() => Versions.MaxBy(static v => v.SavedAt)!;
        public sealed record Version(DateTime SavedAt, string Hash);
    }
}
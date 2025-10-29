using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Ametrin.Serialization;
using IBS.Core.Serialization;

namespace IBS.Core;

public sealed class BackupV2
{
    public DirectoryInfo Root { get; }
    public DirectoryInfo Storage { get; }
    public FileInfo TreeFile { get; }
    public FileInfo MetaDataFile { get; }
    public BackupMetaData MetaData { get; private set; }
    private readonly DirectoryNodeInfo rootNode;

    private BackupV2(DirectoryInfo root, DirectoryInfo storage, FileInfo metaDataFile, FileInfo treeFile, BackupMetaData metaData, DirectoryNodeInfo rootNode)
    {
        Root = root;
        Storage = storage;
        TreeFile = treeFile;
        MetaDataFile = metaDataFile;
        MetaData = metaData;
        this.rootNode = rootNode;
    }

    private const string EMPTY_FILE = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
    public async Task Backup(string path, DirectoryInfo origin)
    {
        DirectoryNotFoundException.ExistsOrThrow(origin);

        var source = origin.File(path);
        FileNotFoundException.ExistsOrThrow(source);

        using var stream = source.OpenRead();
        var hash = Convert.ToHexStringLower(stream.ComputeSHA256Hash());
        var destination = GetFileInfoFromHash(hash, source.Extension);

        var node = GetOrCreateFile(path);
        var info = node.Info;
        info.DeletedAt = null;
        if (info.GetLatest()?.Hash == hash)
        {
            Debug.Assert(destination.Exists);
            // Debug.Assert(source.LastWriteTimeUtc == destination.LastWriteTimeUtc); // with duplicate files this won't match
            Debug.Assert(source.Length == destination.Length);
        }
        else
        {
            info.Versions.Add(new(DateTime.Now, hash));
        }

        if (!destination.Exists)
        {
            destination.Directory!.Create();
            await source.CopyToAsync(destination);
        }
    }

    public ErrorState SoftDelete(FileInfo file)
    {
        if (!BelongsHere(file)) throw new ArgumentException("Cannot process files outside of the current backup", nameof(file));

        var path = file.GetRelativePath(Storage);
        if (!GetFile(path).Branch(out var node))
        {
            return new FileNotFoundException(null, file.FullName);
        }

        return SoftDelete(node);
    }

    private ErrorState SoftDelete(FileNode node)
    {
        Debug.Assert(node.Backup == this);
        var info = node.Info;
        if (info.DeletedAt is DateTime) return new InvalidOperationException("Cannot delete an already deleted file");

        info.DeletedAt = DateTime.Now;
        // var newest = info.Versions.MaxBy(static v => v.SavedAt)!;
        // info.Versions.Clear();
        // info.Versions.Add(newest);

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
        return GetFile(path).Map(IsSoftDeleted).Or(false);
    }

    public bool IsSoftDeleted(FileNode node)
    {
        Debug.Assert(node.Backup == this);
        return node.Info.DeletedAt is not null;
    }

    public bool BelongsHere(FileInfo file) => file.FullName.StartsWith(Storage.FullName, StringComparison.OrdinalIgnoreCase);

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await JsonSerializer.SerializeToFileAsync(MetaDataFile, MetaData, BackupJsonContext.Default.BackupMetaData, overwrite: true, cancellationToken);
        await JsonSerializer.SerializeToFileAsync(TreeFile, rootNode, BackupJsonContext.Default.DirectoryNodeInfo, overwrite: true, cancellationToken);
    }

    public IEnumerable<FileNode> GetFiles(string path)
    {
        return GetDirectory(path)
              .Map(node => node.Files.Select(child => FileNode.Create(this, path, child.Key, child.Value!)))
              .Or([]);
    }

    public IEnumerable<(string name, DirectoryNodeInfo node)> GetDirectories(string path)
    {
        return GetDirectory(path)
              .Map(static node => node.Directories.Select(static pair => (pair.Key, pair.Value)))
              .Or([]);
    }

    private static readonly SearchValues<char> PathSplitChars = SearchValues.Create([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
    public Option<FileNode> GetFile(string path)
    {
        using var nodeNameRanges = path.SplitAny(PathSplitChars);
        var node = rootNode;

        nodeNameRanges.MoveNext();

        while (true)
        {
            var range = nodeNameRanges.Current;
            var hasMore = nodeNameRanges.MoveNext();
            if (hasMore)
            {
                if (!node.TryGetDirectory(path[range], out node))
                {
                    return default;
                }
            }
            else
            {
                if (node.TryGetFile(path[range], out var file))
                {
                    return FileNode.Create(this, path, file);
                }
                else
                {
                    return default;
                }
            }
        }
    }
    
    public FileNode GetOrCreateFile(string path)
    {
        using var nodeNameRanges = path.SplitAny(PathSplitChars);
        var node = rootNode;

        nodeNameRanges.MoveNext();

        while (true)
        {
            var range = nodeNameRanges.Current;
            var hasMore = nodeNameRanges.MoveNext();
            if (hasMore)
            {
                node = node.GetOrCreateDirectory(path[range]);
            }
            else
            {
                return FileNode.Create(this, path, node.GetOrCreateFile(path[range]));
            }
        }
    }

    public Option<DirectoryNodeInfo> GetDirectory(string path)
    {
        if (path is ".") return rootNode;
        using var nodeNameRanges = path.SplitAny(PathSplitChars);
        var node = rootNode;

        foreach (var range in nodeNameRanges)
        {
            if (!node.TryGetDirectory(path[range], out node))
            {
                return default;
            }
        }

        return node;
    }

    public Option<DirectoryNodeInfo> GetOrCreateDirectory(string path)
    {
        if (path is ".") return rootNode;
        using var nodeNameRanges = path.SplitAny(PathSplitChars);
        var node = rootNode;

        foreach (var range in nodeNameRanges)
        {
            node = node.GetOrCreateDirectory(path[range]);
        }

        return node;
    }

    internal FileInfo GetFileInfoFromHash(string hash, string extension)
    {
        var folderName = $"d-{hash.AsSpan(..3)}";
        var fileName = $"f-{hash}{extension}";
        return Storage.Directory(folderName).File(fileName);
    }

    public static async Task<BackupV2> CreateAsync(DirectoryInfo root)
    {
        var storage = root.Directory("_Storage");
        var metaDataFile = root.File("metadata.json");
        var treeFile = root.File("tree.json");

        var metaData = metaDataFile.Exists ? await JsonSerializer.DeserializeAsync(metaDataFile, BackupJsonContext.Default.BackupMetaData) : new() { Version = 2 };
        var rootNode = treeFile.Exists ? await JsonSerializer.DeserializeAsync(treeFile, BackupJsonContext.Default.DirectoryNodeInfo) : new DirectoryNodeInfo();

        return new BackupV2(root, storage, metaDataFile, treeFile, metaData, rootNode);
    }

    public sealed class FileNode
    {
        public BackupV2 Backup { get; }
        public string Name { get; }
        public string Path { get; }
        public FileNodeInfo Info { get; private set; }

        private FileNode(BackupV2 backup, string name, string path, FileNodeInfo info)
        {
            Backup = backup;
            Name = name;
            Path = path;
            Info = info;
        }

        public FileInfo GetFileInfo() => Backup.GetFileInfoFromHash(Info.GetLatest()!.Hash, System.IO.Path.GetExtension(Name));

        public void SoftDelete()
        {
            Backup.SoftDelete(this);
        }

        internal static FileNode Create(BackupV2 backup, string path, FileNodeInfo info) => new(backup, System.IO.Path.GetFileName(path)!, path, info);
        internal static FileNode Create(BackupV2 backup, string parentPath, string name, FileNodeInfo info)
        {
            var path = System.IO.Path.Join(parentPath, name);
            return new(backup, name, path, info);
        }
    }

    public sealed class FileNodeInfo(List<FileNodeInfo.Version> versions, DateTime? deletedAt) : NodeInfo
    {
        public List<Version> Versions { get; set; } = versions ?? [];
        public DateTime? DeletedAt { get; set; } = deletedAt;
        public Version? GetLatest() => Versions.MaxBy(static v => v.SavedAt)!;
        public sealed record Version(DateTime SavedAt, string Hash);
    }

    public sealed class DirectoryNodeInfo : NodeInfo
    {
        public Dictionary<string, DirectoryNodeInfo> Directories { get; init; } = [];
        public Dictionary<string, FileNodeInfo> Files { get; init; } = [];

        public bool TryGetFile(ReadOnlySpan<char> name, [MaybeNullWhen(false)] out FileNodeInfo file)
            => Files.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(name, out file);

        public bool TryGetDirectory(ReadOnlySpan<char> name, [MaybeNullWhen(false)] out DirectoryNodeInfo dir)
            => Directories.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(name, out dir);

        public DirectoryNodeInfo GetOrCreateDirectory(ReadOnlySpan<char> name)
        {
            if (!TryGetDirectory(name, out var dir))
            {
                dir = new();
                Directories.Add(name.ToString(), dir);
            }

            return dir;
        }

        public FileNodeInfo GetOrCreateFile(ReadOnlySpan<char> name)
        {
            if (!TryGetFile(name, out var file))
            {
                file = new([], null);
                Files.Add(name.ToString(), file);
            }

            return file;
        }
    }

    public abstract class NodeInfo;
}
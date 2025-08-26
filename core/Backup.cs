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
        JsonExtensions.WriteToJsonFile(MetaData, MetaDataFile, BackupJsonContext.Default.BackupMetaData);
        JsonExtensions.WriteToJsonFile(DeletedTimeStamps, DeletedTimeStampsFile, BackupJsonContext.Default.DictionaryStringDateTime);
    }

    public static Backup Create(DirectoryInfo root)
    {
        var storage = root.Directory("_Storage");
        var metaDataFile = root.File("metadata.json");
        var deletedTimestampsFile = root.File("deleted.json");

        var metaData = metaDataFile.Exists ? JsonExtensions.ReadFromJsonFile(metaDataFile, BackupJsonContext.Default.BackupMetaData).OrThrow() : new();
        var deletedTimestamps = deletedTimestampsFile.Exists ? JsonExtensions.ReadFromJsonFile(deletedTimestampsFile, BackupJsonContext.Default.DictionaryStringDateTime).OrThrow().ToDictionary(StringComparer.OrdinalIgnoreCase) : new(StringComparer.OrdinalIgnoreCase);

        return new Backup(root, storage, metaDataFile, deletedTimestampsFile, metaData, deletedTimestamps);
    }
}

public sealed class BackupMetaData
{
    public DateTime LastWriteTime { get; set; } = DateTime.MinValue;
}

using System.Diagnostics;
using Ametrin.Serialization;

namespace IBS.Core;

public sealed class Backup(DirectoryInfo root, DirectoryInfo storage, FileInfo deletedMetaData, Dictionary<string, DateTime> deletedTimeStamps)
{
    public const string DELETED_EXTENSION = ".deleted";

    public DirectoryInfo Root { get; } = root;
    public DirectoryInfo Storage { get; } = storage;
    public FileInfo DeletedMetaData { get; } = deletedMetaData;
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
        if(!BelongsHere(file)) throw new ArgumentException("Cannot process files outside of the current backup", nameof(file));

        if (file.Extension is DELETED_EXTENSION && !DeletedTimeStamps.ContainsKey(file.FullName))
        {
            DeletedTimeStamps[file.FullName] = DateTime.Now;
        }

        return file.Extension is DELETED_EXTENSION;
    }

    public bool BelongsHere(FileInfo file) => file.FullName.StartsWith(Storage.FullName);

    public static Backup Create(DirectoryInfo root)
    {
        var storage = root.Directory("_Storage");
        var deletedMetaData = root.File("deleted.json");

        return new Backup(root, storage, deletedMetaData, deletedMetaData.Exists ? JsonExtensions.ReadFromJsonFile<Dictionary<string, DateTime>>(deletedMetaData).OrThrow() : []);
    }

}
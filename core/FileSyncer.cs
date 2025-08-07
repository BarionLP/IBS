using System.Collections.Immutable;
using System.Diagnostics;

namespace IBS.Core;

public static class FileSyncer
{
    public static void AdvancedSync(BackupConfig config, IProgress<float>? progress = null, IProgress<string>? workingOn = null)
    {
        var backups = config.BackupDirectories.Where(static b => b.Exists).Select(Backup.Create).ToImmutableArray();
        Sync(config.OriginDirectory);

        var now = DateTime.Now;
        foreach (var backup in backups)
        {
            foreach (var (deleted, timestamp) in backup.DeletedTimeStamps)
            {
                if (!File.Exists(deleted))
                {
                    backup.DeletedTimeStamps.Remove(deleted);
                }
            }
            backup.MetaData.LastWriteTime = DateTime.Now;
            backup.Save();
        }

        void Sync(DirectoryInfo directory)
        {
            var relativeDirectory = directory.GetRelativePath(config.OriginDirectory);
            var files = GetFiles(directory);

            // read all files in the backup
            var backupInfos = backups.Select(backup =>
            {
                var dir = backup.Storage.Directory(relativeDirectory);
                dir.CreateIfNotExists();
                return (backup, files: GetFiles(dir).Where(f => !backup.IsSoftDeleted(f)).ToList());
            }).ToArray();

            // sync
            foreach (var file in files)
            {
                var relativeFilePath = file.GetRelativePath(config.OriginDirectory);
                foreach (var info in backupInfos)
                {
                    SyncFile(file, info.backup.Storage.File(relativeFilePath));
                    info.files.RemoveAll(f => string.Equals(f.GetRelativePath(info.backup.Storage), relativeFilePath, StringComparison.OrdinalIgnoreCase));
                }
            }

            // mark remaining files as deleted
            foreach (var info in backupInfos)
            {
                foreach (var file in info.files)
                {
                    info.backup.SoftDelete(file);
                }
            }

            // sync subdirectories
            var subDirectories = directory.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).Where(config.ShouldInclude).ToArray();
            subDirectories.Consume(Sync);

            // mark remaining directories as deleted
            foreach (var info in backupInfos)
            {
                var deletedDirectories = info.backup.Storage.Directory(relativeDirectory)
                    .EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
                    .Where(backupDir => !subDirectories.Any(originDir =>  string.Equals(originDir.GetRelativePath(config.OriginDirectory), backupDir.GetRelativePath(info.backup.Storage), StringComparison.OrdinalIgnoreCase)));

                foreach (var deletedDirectory in deletedDirectories)
                {
                    foreach (var file in deletedDirectory.EnumerateFiles("*", SearchOption.AllDirectories).Where(f => !info.backup.IsSoftDeleted(f)))
                    {
                        info.backup.SoftDelete(file);
                    }
                }
            }
        }

        IEnumerable<FileInfo> GetFiles(DirectoryInfo directory)
            => directory.Exists ? directory.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Where(config.ShouldInclude) : [];

        void SyncFile(FileInfo from, FileInfo to)
        {
            Debug.Assert(from.Exists);

            if (!to.Exists || !AreFilesInSync(from, to))
            {
                workingOn?.Report(from.FullName);
                if (to.Exists)
                {
                    to.Delete();
                }
                from.CopyTo(to, overwrite: true);
            }
        }
    }

    public static bool AreFilesInSync(FileInfo mainFileInfo, FileInfo backupFileInfo)
        => backupFileInfo.Exists && mainFileInfo.Length == backupFileInfo.Length && mainFileInfo.LastWriteTimeUtc == backupFileInfo.LastWriteTimeUtc;
}

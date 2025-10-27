using System.Diagnostics;

namespace IBS.Core;

public static class FileSyncer
{
    public static async Task<ErrorState> SyncV2(BackupConfig config, IProgress<float>? progress = null, IProgress<string>? workingOn = null, CancellationToken token = default)
    {
        var backups = await Task.WhenAll(config.BackupDirectories.Where(static b => b.Exists).Select(BackupV2.CreateAsync));

        await SyncImplAsync(config.OriginDirectory);

        await Task.WhenAll(backups.Select(b =>
        {
            b.MetaData.LastWriteTime = DateTime.Now;
            return b.SaveAsync(token);
        }));

        return default;

        async Task SyncImplAsync(DirectoryInfo directory)
        {
            // var path = directory.GetRelativePath(config.OriginDirectory);
            var existingNodes = new HashSet<string>();

            foreach (var file in GetFiles(directory))
            {
                workingOn?.Report(file.FullName);
                existingNodes.Add(file.Name);
                var path = file.GetRelativePath(config.OriginDirectory);

                foreach (var backup in backups)
                {
                    await backup.Backup(path, config.OriginDirectory);
                }
            }

            var dirPath = directory.GetRelativePath(config.OriginDirectory);
            foreach (var backup in backups)
            {
                foreach (var file in backup.GetFiles(dirPath))
                {
                    if (existingNodes.Contains(file.Name) || file.Info.DeletedAt is not null) continue;
                    file.SoftDelete();
                }
            }

            // sync subdirectories
            existingNodes.Clear();
            var subDirectories = directory.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).Where(config.ShouldInclude).ToArray();

            foreach (var sub in subDirectories)
            {
                existingNodes.Add(sub.Name);
                await SyncImplAsync(sub);
            }

            // mark remaining directories as deleted
            foreach (var backup in backups)
            {
                foreach (var (name, node) in backup.GetDirectories(dirPath))
                {
                    if (existingNodes.Contains(name)) continue;
                    SoftDeleteNodes(backup, dirPath is "." ? name : Path.Join(dirPath, name));
                }
            }

            static void SoftDeleteNodes(BackupV2 backup, string dirPath)
            {
                foreach (var file in backup.GetFiles(dirPath))
                {
                    file.SoftDelete();
                }

                foreach (var (name, node) in backup.GetDirectories(dirPath))
                {
                    SoftDeleteNodes(backup, Path.Join(dirPath, name));
                }
            }
        }


        IEnumerable<FileInfo> GetFiles(DirectoryInfo directory)
            => directory.Exists ? directory.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Where(config.ShouldInclude) : [];
    }

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
                    .Where(backupDir => !subDirectories.Any(originDir => string.Equals(originDir.GetRelativePath(config.OriginDirectory), backupDir.GetRelativePath(info.backup.Storage), StringComparison.OrdinalIgnoreCase)));

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
                from.CopyTo(to, overwrite: true);
            }
        }
    }

    public static bool AreFilesInSync(FileInfo mainFileInfo, FileInfo backupFileInfo)
        => backupFileInfo.Exists && mainFileInfo.Length == backupFileInfo.Length && mainFileInfo.LastWriteTimeUtc == backupFileInfo.LastWriteTimeUtc;
}

public static class FileOperations
{
    extension(File)
    {
        public static async Task CopyAsync(string sourceFileName, string destFileName, bool overwrite = false, CancellationToken token = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceFileName);
            ArgumentException.ThrowIfNullOrWhiteSpace(destFileName);

            if (File.Exists(destFileName) && !overwrite)
            {
                throw new IOException();
            }

            using var source = File.OpenRead(sourceFileName);
            using var destination = File.Create(destFileName);

            await source.CopyToAsync(destination, token);
        }

        public static Task CopyAsync(FileInfo sourceFileInfo, FileInfo destFileInfo, bool overwrite = false, CancellationToken token = default)
        {
            if (destFileInfo.Exists && !overwrite)
            {
                throw new IOException();
            }

            return Task.Run(() => sourceFileInfo.CopyTo(destFileInfo), token);
        }
    }

    extension(FileInfo fileInfo)
    {
        public Task CopyToAsync(FileInfo destFileInfo, bool overwrite = false, CancellationToken token = default) => File.CopyAsync(fileInfo, destFileInfo, overwrite, token);
    }
}
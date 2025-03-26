using System.Diagnostics;

namespace IBS.Core;

public static class FileSyncer
{
    public static void SimpleSync(BackupConfig config)
    {
        var files = config.OriginInfo.EnumerateFiles("*", SearchOption.AllDirectories).Where(config.ShouldInclude);
        foreach (var file in files)
        {
            var relativePath = file.GetRelativePath(config.OriginInfo);
            foreach (var backup in config.BackupInfos)
            {
                SyncFile(file, backup.File(relativePath));
            }
        }
    }

    private const string DELETED_EXTENSION = ".deleted";
    public static void AdvancedSync(BackupConfig config, Progress<float> progress, Progress<string> workingOn)
    {
        Sync(config.OriginInfo);

        void Sync(DirectoryInfo directory)
        {
            var relativeDirectory = directory.GetRelativePath(config.OriginInfo);
            var files = GetFiles(directory);

            var backupFiles = config.BackupInfos.Where(static b => b.Exists).Select(b =>
            {
                var dir = b.Directory(relativeDirectory);
                dir.CreateIfNotExists();
                return (location: b, files: GetFiles(dir).Where(static f => f.Extension is not DELETED_EXTENSION).ToList());
            }).ToArray();

            foreach (var file in files)
            {
                var relativeFilePath = file.GetRelativePath(config.OriginInfo);
                foreach (var backup in backupFiles)
                {
                    SyncFile(file, backup.location.File(relativeFilePath));
                    backup.files.RemoveAll(f => f.GetRelativePath(backup.location) == relativeFilePath);
                }
            }

            foreach (var file in backupFiles.SelectMany(static s => s.files))
            {
                Console.WriteLine($"marked {file.Name} deleted");
                file.MoveTo($"{file.FullName}{DELETED_EXTENSION}");
            }

            var subDirectories = directory.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).Where(config.ShouldInclude).ToArray();
            subDirectories.Consume(Sync);

            foreach (var backup in backupFiles)
            {
                var deletedDirectories = backup.location.Directory(relativeDirectory)
                    .EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
                    .Where(d => !subDirectories.Any(o => o.GetRelativePath(directory) == d.GetRelativePath(backup.location)));

                foreach (var deletedDirectory in deletedDirectories)
                {
                    foreach (var file in deletedDirectory.EnumerateFiles("*", SearchOption.AllDirectories))
                    {
                        Console.WriteLine($"marked {file.Name} deleted");
                        file.MoveTo($"{file.FullName}{DELETED_EXTENSION}");
                    }
                }
            }
        }

        IEnumerable<FileInfo> GetFiles(DirectoryInfo directory) => directory.Exists ? directory.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Where(config.ShouldInclude) : [];
    }

    private static void SyncFile(FileInfo from, FileInfo to)
    {
        Debug.Assert(from.Exists);

        if (!to.Exists || !AreFilesInSync(from, to))
        {
            if (to.Exists)
            {
                to.Delete();
            }
            from.CopyTo(to, overwrite: true);
            Console.WriteLine($"Synced {from.Name}");
        }
    }

    public static bool AreFilesInSync(FileInfo mainFileInfo, FileInfo backupFileInfo)
        => backupFileInfo.Exists && mainFileInfo.Length == backupFileInfo.Length && mainFileInfo.LastWriteTimeUtc == backupFileInfo.LastWriteTimeUtc;
}

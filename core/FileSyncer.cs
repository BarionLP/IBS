using System.Diagnostics;

namespace IBS.Core;

public static class FileSyncer
{
    private const string DELETED_EXTENSION = ".deleted";
    public static void AdvancedSync(BackupConfig config, IProgress<float> progress, IProgress<string> workingOn)
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
                    .Where(d => !subDirectories.Any(o => o.GetRelativePath(config.OriginInfo) == d.GetRelativePath(backup.location)));

                foreach (var deletedDirectory in deletedDirectories)
                {
                    foreach (var file in deletedDirectory.EnumerateFiles("*", SearchOption.AllDirectories).Where(static f => f.Extension is not DELETED_EXTENSION))
                    {
                        Console.WriteLine($"marked {file.Name} deleted");
                        file.MoveTo($"{file.FullName}{DELETED_EXTENSION}");
                    }
                }
            }
        }

        IEnumerable<FileInfo> GetFiles(DirectoryInfo directory) => directory.Exists ? directory.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Where(config.ShouldInclude) : [];
        
        void SyncFile(FileInfo from, FileInfo to)
        {
            Debug.Assert(from.Exists);

            if (!to.Exists || !AreFilesInSync(from, to))
            {
                workingOn.Report(from.FullName);
                if (to.Exists)
                {
                    to.Delete();
                }
                from.CopyTo(to, overwrite: true);
                Console.WriteLine($"Synced {from.Name}");
            }
        }
    }

    public static bool AreFilesInSync(FileInfo mainFileInfo, FileInfo backupFileInfo)
        => backupFileInfo.Exists && mainFileInfo.Length == backupFileInfo.Length && mainFileInfo.LastWriteTimeUtc == backupFileInfo.LastWriteTimeUtc;
}

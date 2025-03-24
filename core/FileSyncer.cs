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

    public static void AdvancedSync(BackupConfig config)
    {
        Sync(config.OriginInfo);

        void Sync(DirectoryInfo directory)
        {
            var relativeOriginPath = directory.GetRelativePath(config.OriginInfo);
            var files = GetFiles(directory);
            var backupFiles = config.BackupInfos.Select(b => (location: b, files: GetFiles(b.Directory(relativeOriginPath)).Where(f => f.Extension is not ".deleted").ToList())).ToArray();

            foreach (var file in files)
            {
                var relativeFilePath = file.GetRelativePath(config.OriginInfo);
                foreach (var backup in backupFiles)
                {
                    Console.WriteLine($"Synced {relativeFilePath}");
                    //SyncFile(file, backup.location.File(relativeFilePath));
                    backup.files.RemoveAll(f => f.GetRelativePath(backup.location) == relativeFilePath);
                }
            }

            foreach (var file in backupFiles.SelectMany(static s => s.files))
            {
                Console.WriteLine($"marked {file.Name} deleted");
                //file.MoveTo($"{file.Name}.deleted");
            }

            foreach (var sub in directory.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
            {
                Sync(sub);
            }
        }

        IEnumerable<FileInfo> GetFiles(DirectoryInfo directory) => directory.Exists ? directory.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Where(config.ShouldInclude) : [];
    }

    private static void SyncFile(FileInfo from, FileInfo to)
    {
        Debug.Assert(from.Exists);

        if (!to.Exists || !AreFilesInSync(from, to))
        {
            from.CopyTo(to);
        }
    }

    public static bool AreFilesInSync(FileInfo mainFileInfo, FileInfo backupFileInfo)
        => backupFileInfo.Exists && mainFileInfo.Length == backupFileInfo.Length && mainFileInfo.LastWriteTimeUtc == backupFileInfo.LastWriteTimeUtc;
}

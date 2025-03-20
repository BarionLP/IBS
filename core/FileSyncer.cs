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
        var origin = config.OriginInfo.EnumerateFiles("*", SearchOption.AllDirectories).Where(config.ShouldInclude).GetEnumerator();
        var backups = config.BackupInfos.Select(b => b.EnumerateFiles("*", SearchOption.AllDirectories).Where(config.ShouldInclude).GetEnumerator()).ToArray();

        foreach (var backup in backups)
        {
            backup.MoveNext();
        }

        while (origin.MoveNext())
        {
            var relativePath = origin.Current.GetRelativePath(config.OriginInfo);
            foreach (var backup in backups)
            {
                if (backup.Current.FullName.EndsWith(relativePath))
                {
                    //SyncFile(origin.Current, backup.Current);
                    Console.WriteLine($"Synced {origin.Current.FullName}");
                    backup.MoveNext();
                }
            }
        }
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

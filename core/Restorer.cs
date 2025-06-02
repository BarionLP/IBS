using System.Collections.Immutable;

namespace IBS.Core;

public static class Restorer
{
    public static ErrorState RestoreBackup(BackupConfig config, DirectoryInfo targetDirectory, IProgress<float> progress, IProgress<string> workingOn)
    {
        var backups = config.BackupDirectories.Where(static b => b.Exists).Select(Backup.Create).ToImmutableArray();
        if (backups.IsEmpty) return new ArgumentException("No Backup found", nameof(config));
        var source = backups.OrderByDescending(static backup => backup.MetaData.LastWriteTime).First();

        source.Storage.ForeachFile(file =>
        {
            if (source.IsSoftDeleted(file)) return;

            var targetFile = targetDirectory.File(file.GetRelativePath(source.Storage));
            if (targetFile.Exists) return;

            workingOn.Report(targetFile.FullName);

            file.CopyTo(targetFile);
        }, progress);

        return default;
    }
}

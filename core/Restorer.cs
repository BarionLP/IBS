using System.Diagnostics;

namespace IBS.Core;

public static class Restorer
{
    public static Task RestoreV2(BackupV2 backup, DirectoryInfo target)
    {
        return RestoreImplAsync(backup.GetDirectory(".").OrThrow(), target);

        async Task RestoreImplAsync(BackupV2.DirectoryNodeInfo directory, DirectoryInfo target)
        {
            if (directory.Files.Any(static f => !f.Value.IsSoftDeleted))
            {
                target.CreateIfNotExists();

                foreach (var (name, file) in directory.Files)
                {
                    if (file.IsSoftDeleted) continue;

                    var latest = file.GetLatest();
                    Debug.Assert(latest is not null);
                    if (latest is not null)
                    {
                        var targetFile = target.File(name);
                        Debug.Assert(!targetFile.Exists);

                        var backupFile = backup.GetFileInfoFromHash(latest.Hash, Path.GetExtension(name));
                        Debug.Assert(backupFile.Exists);
                        await backupFile.CopyToAsync(targetFile, overwrite: false);
                    }
                }
            }

            foreach (var (name, subDir) in directory.Directories)
            {
                await RestoreImplAsync(subDir, target.Directory(name));
            }
        }

    }

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

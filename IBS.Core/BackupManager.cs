using System.Diagnostics;

namespace IBS.Core;

public static class BackupManager
{
    public static event Action? OnConfigChanged;
    public static BackupHandler? Handler;

    public static void SetHandler(BackupHandler config)
    {
        Handler = config;
        OnConfigChanged?.Invoke();
    }

    public static Option SyncBackup(IProgress<float>? progress = null, IProgress<string>? workingOn = null)
    {
        if (Handler is null)
            return false;
        Handler.RecreateFolderStructure();

        Handler.ForeachFile(file =>
        {
            if (file.IsSynced())
                return;

            workingOn?.Report(file.RelativePath);
            file.Sync();
        }, progress);
        return true;
    }

    public static Option CleanBackup(IProgress<float>? progress = null, IProgress<string>? workingOn = null)
    {
        if (Handler is null)
            return false;

        //Handler.ForeachBackupFolder((backupFolderInfo, backupInfo) => {
        //    var originInfo = Handler.Config.GetFolderInfo(backupFolderInfo.GetRelativePath(backupInfo));

        //    if(originInfo.Exists && Handler.Config.ShouldInclude(originInfo)) return;

        //    workingOn?.Report(backupFolderInfo.FullName);
        //    Trace.TraceInformation("deleted {0}", backupFolderInfo.FullName);
        //    backupFolderInfo.Trash();
        //});

        Handler.ForeachBackupFile((backupFileInfo, backupInfo) =>
        {
            var originInfo = Handler.Config.GetFileInfo(backupFileInfo.GetRelativePath(backupInfo));

            if (originInfo.Exists && Handler.Config.ShouldInclude(originInfo))
                return;

            workingOn?.Report(originInfo.FullName);
            Trace.TraceInformation("deleted or excluded {0}", originInfo.FullName);
            backupFileInfo.Trash();
        }, progress);

        return true;
    }

    public static Option VerifyBackup(IProgress<float>? progress = null)
    {
        if (Handler is null)
            return false;

        Handler.ForeachFile(file =>
        {
            if (file.CompareHashes())
                return;
            Trace.TraceWarning("{0} has broken backups or is broken...", file.OriginInfo.FullName);
        }, progress);

        return true;
    }
}
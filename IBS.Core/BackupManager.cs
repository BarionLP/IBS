using Ametrin.Utils;
using System.Diagnostics;

namespace IBS.Core;

public static class BackupManager{
    public static event Action? OnConfigChanged;
    public static BackupHandler? Handler;

    public static void SetConfig(BackupHandler config){
        Handler = config;
        OnConfigChanged?.Invoke();
    }
    
    public static Result SyncBackup(IProgress<(float, string)>? progress = null){
        if(Handler is null) return ResultStatus.Failed;
        Handler.RecreateFolderStructure();

        Handler.ForeachFile(file => file.Sync(), progress);
        return ResultStatus.Succeeded;
    }

    public static Result CleanBackup(IProgress<float>? progress = null) {
        if(Handler is null) return ResultStatus.Failed;

        Handler.ForeachBackupFile((backupFileInfo, backupInfo) => {
            var originInfo = Handler.Config.GetFileInfo(backupFileInfo.GetRelativePath(backupInfo));

            if(originInfo.Exists && Handler.Config.ShouldInclude(originInfo)) return;

            Trace.TraceInformation("deleted or excluded {0}", originInfo.FullName);
            backupFileInfo.Trash();
        } , progress);
        
        return ResultStatus.Succeeded;
    }
    
    public static Result VerifyBackup(IProgress<float>? progress = null) {
        if(Handler is null) return ResultStatus.Failed;

        Handler.ForeachFile(file => {
            if(file.CompareHashes()) return;
            Trace.TraceWarning("{0} has broken backups or is broken...", file.OriginInfo.FullName);
        }, progress);

        return ResultStatus.Succeeded;
    }
}
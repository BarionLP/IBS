using Ametrin.Utils;
using Microsoft.VisualBasic.FileIO;
using System.Diagnostics;

namespace IBS.Core;

public static class BackupManager{
    public static event Action? OnConfigChanged;
    private static BackupHandler? Handler;

    public static void SetConfig(BackupHandler config){
        Handler = config;
        OnConfigChanged?.Invoke();
    }
    
    public static Result SyncBackup(IProgress<(float, string)>? progress = null){
        if(Handler is null) return ResultStatus.Failed;
        Handler.RecreateFolderStructure();

        var files = Handler.GetFiles().ToArray();
        float totalFiles = files.Length;
        var synced = 0;
        foreach(var file in files){
            if(!file.IsSynced()) {
                progress?.Report((synced/totalFiles, file.OriginInfo.FullName));
                file.Sync();
            }
            synced++;
        }
        return ResultStatus.Succeeded;
    }

    public static Result CleanBackup(IProgress<float>? progress = null) {
        if(Handler is null) return ResultStatus.Failed;

        var files = Handler.GetFilesFromBackup().ToArray();
        float totalFiles = files.Length;
        var synced = 0;
        foreach(var file in files) {
            synced++;
            progress?.Report(synced / totalFiles);
            if(file.IsDeleted()) {
                FileSystem.DeleteFile(file.BackupInfo.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                Trace.TraceInformation("deleted {0}", file.OriginInfo.FullName);
                continue;
            }
            if(!file.CompareHashes()) Trace.TraceWarning("{0} and {1} do not match", file.OriginInfo.FullName, file.BackupInfo.FullName);
        }
        return ResultStatus.Succeeded;
    }
}
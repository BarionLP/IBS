using Ametrin.Utils;

namespace IBS.Core;

public static class BackupManager{
    public static event Action? OnConfigChanged;
    private static BackupConfig? Config;

    public static void SetConfig(BackupConfig config){
        Config = config;
        OnConfigChanged?.Invoke();
    }

    public static Result SyncBackup(){
        
        return ResultStatus.Succeeded;
    }
}

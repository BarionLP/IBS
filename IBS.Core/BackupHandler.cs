using Ametrin.Utils;
using Ametrin.Utils.Optional;

namespace IBS.Core;

public sealed class BackupHandler(IBackupConfig backupConfig) {
    public IBackupConfig Config { get; set; } = backupConfig;

    public BackedupFile GetFile(string relativePath){
        return new (Config, relativePath);
    }

    public BackedupFile GetFile(FileInfo origin){
        return GetFile(origin, Config.OriginInfo);
    }
    public BackedupFile GetFile(FileInfo fileInfo, DirectoryInfo relativeTo){
        return GetFile(fileInfo.GetRelativePath(relativeTo));
    }

    public IEnumerable<BackedupFile> GetFiles() => Config.GetFiles().Select(GetFile);
    public IEnumerable<FileInfo> GetBackupFiles() {
        foreach(var backupInfo in Config.BackupInfos) {
            if(!backupInfo.Exists) continue;
            foreach(var backupFileInfo in backupInfo.EnumerateFiles()) {
                yield return backupFileInfo;
            }
        }
    }

    public ResultFlag RecreateFolderStructure() {
        Config.ForeachBackup(backupInfo => {
            foreach(var directory in Config.GetDirectories()) {
                var backupDirectory = new DirectoryInfo(Path.Combine(backupInfo.FullName, directory.GetRelativePath(Config.OriginInfo)));
                if(!backupDirectory.Exists) backupDirectory.Create();
            }
        });
        
        return ResultFlag.Succeeded;
    }
    
    public void ForeachFile(Action<BackedupFile> action, IProgress<float>? progress) {
        if(progress is null) {
            ForeachFile(action);
            return;
        }

        var files = GetFiles().ToArray();
        float totalFiles = files.Length;
        var processed = 0;
        foreach(var file in files) {
            action(file);
            processed++;
            progress.Report(processed / totalFiles);
        }
    }

    public void ForeachFile(Action<BackedupFile> action) {
        foreach(var file in GetFiles()) {
            action(file);
        }
    }

    public void ForeachBackupFolder(Action<DirectoryInfo, DirectoryInfo> action, IProgress<float>? progress) {
        if(progress is null) {
            ForeachBackupFolder(action);
            return;
        }

        var completed = 0f;
        foreach(var backupInfo in Config.BackupInfos) {
            if(!backupInfo.Exists) continue;

            var backupFolders = backupInfo.GetDirectories("*", SearchOption.AllDirectories);
            float totalFiles = backupFolders.Length;
            var synced = 0;
            foreach(var backupFolderInfo in backupFolders) {
                action(backupFolderInfo, backupInfo);
                synced++;
                progress?.Report(completed + (synced / totalFiles / Config.BackupInfos.Count));
            }
            completed += 1 / Config.BackupInfos.Count;
        }
    }

    public void ForeachBackupFolder(Action<DirectoryInfo, DirectoryInfo> action) {
        foreach(var backupInfo in Config.BackupInfos) {
            if(!backupInfo.Exists) continue;

            foreach(var backupFolderInfo in backupInfo.GetDirectories("*", SearchOption.AllDirectories)) {
                action(backupFolderInfo, backupInfo);
            }
        }
    }


    public void ForeachBackupFile(Action<FileInfo, DirectoryInfo> action, IProgress<float>? progress) {
        if(progress is null) {
            ForeachBackupFile(action);
            return;
        }

        var completed = 0f;
        foreach(var backupInfo in Config.BackupInfos) {
            if(!backupInfo.Exists) continue;

            var backupFiles = backupInfo.GetFiles("*", SearchOption.AllDirectories);
            float totalFiles = backupFiles.Length;
            var synced = 0;
            foreach(var backupFileInfo in backupFiles) {
                action(backupFileInfo, backupInfo);
                synced++;
                progress?.Report(completed + (synced / totalFiles / Config.BackupInfos.Count));
            }
            completed += 1 / Config.BackupInfos.Count;
        }
    }
    
    public void ForeachBackupFile(Action<FileInfo, DirectoryInfo> action) {
        foreach(var backupInfo in Config.BackupInfos) {
            if(!backupInfo.Exists) continue;

            foreach(var backupFileInfo in backupInfo.EnumerateFiles("*", SearchOption.AllDirectories)) {
                action(backupFileInfo, backupInfo);
            }
        }
    }
}

using Ametrin.Utils;

namespace IBS.Core;

public sealed class BackedupFile{

    public FileInfo OriginInfo { get; }
    public FileInfo[] BackupInfos { get; }
    public string RelativePath { get; }
    
    private BackedupFile(FileInfo originInfo, FileInfo[] backupInfos, string relativePath){
        OriginInfo = originInfo;
        BackupInfos = backupInfos;
        RelativePath = relativePath;
    }

    internal BackedupFile(IBackupConfig config, string relativePath) : this(config.GetFileInfo(relativePath), config.GetBackupFiles(relativePath).ToArray(), relativePath) {}

    public bool IsNew() => OriginInfo.Exists && !BackupInfos.Any(b => b.Exists);
    public bool IsDeleted() => !OriginInfo.Exists;
    public bool IsSynced() => FileSyncHelper.AreFilesInSync(OriginInfo, BackupInfos);
    public bool CompareHashes() => FileSyncHelper.CompareHashes(OriginInfo, BackupInfos);
    public State GetState(){
        if(IsDeleted()) return State.Deleted;
        if(IsNew()) return State.New;
        if(!IsSynced()) return State.Changed;
        return State.Synced;
    }

    public bool IsNew(FileInfo backedupFile) => OriginInfo.Exists && !backedupFile.Exists;
    public bool IsSynced(FileInfo backedupFile) => FileSyncHelper.AreFilesInSync(OriginInfo, backedupFile);
    public bool CompareHashes(FileInfo backedupFile) => OriginInfo.CompareHash(backedupFile);
    public State GetState(FileInfo backedupFile){
        if(IsDeleted()) return State.Deleted;
        if(IsNew(backedupFile)) return State.New;
        if(!IsSynced(backedupFile)) return State.Changed;
        return State.Synced;
    }

    public void RemoveBackups() {
        foreach(var backupInfo in BackupInfos) {
            if(!backupInfo.Exists) continue;
            backupInfo.Trash();
        }
    }

    public Result Sync(){
        try{
            if(IsDeleted()) {
                RemoveBackups();
                return ResultStatus.Succeeded;
            }

            foreach(var backupInfo in BackupInfos) {
                if(IsSynced(backupInfo)) continue;
                OriginInfo.CopyTo(backupInfo, true);
            }
            return ResultStatus.Succeeded;
        } catch{
            return ResultStatus.Failed;
        }
    }

    public enum State { New, Synced, Changed, Deleted };
}

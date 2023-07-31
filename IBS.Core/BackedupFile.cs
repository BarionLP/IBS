using Ametrin.Utils;

namespace IBS.Core;

public sealed class BackedupFile{

    public FileInfo OriginInfo { get; }
    public FileInfo BackupInfo { get; }
    public string RelativePath { get; }
    
    private BackedupFile(FileInfo originInfo, FileInfo backupInfo, string relativePath){
        if(!originInfo.Exists && !backupInfo.Exists) throw new FileNotFoundException($"file {originInfo.FullName} does not exist in origin or backup");
        OriginInfo = originInfo;
        BackupInfo = backupInfo;
        RelativePath = relativePath;
    }

    internal BackedupFile(IBackupConfig config, string relativePath) : this(new(Path.Combine(config.OriginInfo.FullName, relativePath)), new(Path.Combine(config.BackupInfo.FullName, relativePath)), relativePath) {}

    public bool IsNew() => OriginInfo.Exists && !BackupInfo.Exists;
    public bool IsDeleted() => !OriginInfo.Exists && BackupInfo.Exists;
    public bool IsSynced() => FileSyncHelper.AreFilesInSync(OriginInfo, BackupInfo);
    public bool CompareHashes() => OriginInfo.CompareHash(BackupInfo);
    public State GetState(){
        if(IsNew()) return State.New;
        if(IsDeleted()) return State.Deleted;
        if(!IsSynced()) return State.Changed;
        return State.Synced;
    }
    public Result Sync(){
        try{
            OriginInfo.Refresh();
            BackupInfo.Refresh();
            switch(GetState()){
                case State.New:
                case State.Changed:
                    OriginInfo.CopyTo(BackupInfo, true);
                    break;
                case State.Deleted:
                    BackupInfo.Delete();
                    break;
            }
            return ResultStatus.Succeeded;
        } catch{
            return ResultStatus.Failed;
        }
    }

    public enum State { New, Synced, Changed, Deleted };
}

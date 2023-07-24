using Ametrin.Utils;

namespace IBS.Core;

public record BackedupFile(FileInfo Origin, FileInfo Backup){
    public BackedupFile(string origin, string backup) : this(new FileInfo(origin), new FileInfo(backup)) {}

    public bool IsSynced() => FileSyncHelper.AreFilesInSync(Origin, Backup);
    public Result SyncBackup(){
        
        return ResultStatus.Succeeded;
    }
}

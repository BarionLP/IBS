using Ametrin.Utils;

namespace IBS.Core;

public static class FileSyncHelper{
    public static bool AreFilesInSync(FileInfo mainFileInfo, FileInfo backupFileInfo){
        if(!backupFileInfo.Exists) return false;
        return mainFileInfo.Length == backupFileInfo.Length && mainFileInfo.LastWriteTimeUtc == backupFileInfo.LastWriteTimeUtc;
    }

    public static Result OverrideFile(FileInfo oldFile, FileInfo newFile){
        if(!newFile.Exists) return ResultStatus.PathDoesNotExist;
        
        newFile.CopyTo(oldFile.FullName, true);

        return ResultStatus.Succeeded;
    }
}

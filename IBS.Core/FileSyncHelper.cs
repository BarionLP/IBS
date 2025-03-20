namespace IBS.Core;

public static class FileSyncHelper
{
    public static bool AreFilesInSync(FileInfo mainFileInfo, IEnumerable<FileInfo> backupFileInfos)
    {
        foreach (var backupFileInfo in backupFileInfos)
        {
            if (!AreFilesInSync(mainFileInfo, backupFileInfo))
            {
                return false;
            }
        }
        return true;
    }

    public static bool AreFilesInSync(FileInfo mainFileInfo, FileInfo backupFileInfo)
    {
        return backupFileInfo.Exists && mainFileInfo.Length == backupFileInfo.Length && mainFileInfo.LastWriteTimeUtc == backupFileInfo.LastWriteTimeUtc;
    }

    public static bool CompareHashes(FileInfo mainFileInfo, IEnumerable<FileInfo> backupFileInfos)
    {
        foreach (var backupFileInfo in backupFileInfos)
        {
            if (!mainFileInfo.CompareHash(backupFileInfo))
            {
                return false;
            }
        }

        return true;
    }
}

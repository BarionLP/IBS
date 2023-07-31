using System.Security.Cryptography;

namespace IBS.Core;

public static class FileSyncHelper{
    public static bool AreFilesInSync(FileInfo mainFileInfo, FileInfo backupFileInfo){
        if(!backupFileInfo.Exists) return false;
        return mainFileInfo.Length == backupFileInfo.Length && mainFileInfo.LastWriteTimeUtc == backupFileInfo.LastWriteTimeUtc;
    }

    public static void CopyTo(this FileInfo mainFile, FileInfo newFile, bool overwrite = false){
        mainFile.CopyTo(newFile.FullName, overwrite);
    }

    public static string GetRelativePath(this FileSystemInfo main, DirectoryInfo relativeTo) {
        return Path.GetRelativePath(relativeTo.FullName, main.FullName);
    }

    public static bool CompareHash(this FileInfo self, FileInfo other) {
        return self.ComputeMd5Hash() == other.ComputeMd5Hash();
    }

    public static string ComputeSha256Hash(this FileInfo fileInfo) {
        using var sha256Hash = SHA256.Create();
        using var stream = File.OpenRead(fileInfo.FullName);

        var hash = sha256Hash.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    static string ComputeMd5Hash(this FileInfo fileInfo) {
        using var md5Hash = MD5.Create();
        using var stream = File.OpenRead(fileInfo.FullName);
        
        var hash = md5Hash.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }
}

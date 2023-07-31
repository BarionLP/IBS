using Ametrin.Utils;

namespace IBS.Core;

public sealed class BackupHandler{
    public IBackupConfig Config { get; private set; }

    public BackupHandler(IBackupConfig backupConfig){
        Config = backupConfig;
    }

    public BackedupFile GetFile(string relativePath){
        return new (Config, relativePath);
    }

    public BackedupFile GetFile(FileInfo origin){
        return new(Config, origin.GetRelativePath(Config.OriginInfo));
    }
    public BackedupFile GetFileFromBackup(FileInfo backup){
        return new(Config, backup.GetRelativePath(Config.BackupInfo));
    }

    public IEnumerable<BackedupFile> GetFiles() => Config.GetFiles().Select(GetFile);
    public IEnumerable<BackedupFile> GetFilesFromBackup() => Config.GetFilesFromBackup().Select(GetFileFromBackup);

    public Result RecreateFolderStructure() {
        foreach(var directory in Config.GetDirectories()) {
            var backupDirectory = new DirectoryInfo(Path.Combine(Config.BackupInfo.FullName, directory.GetRelativePath(Config.OriginInfo)));
            if(!backupDirectory.Exists) backupDirectory.Create();
        }
        return ResultStatus.Succeeded;
    }
}

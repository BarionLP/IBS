namespace IBS.Core.Test.Unit;

public sealed class FileSyncTests{
    public const string ORIGIN_PATH = "../../../Data/Origin/";
    public const string BACKUP_PATH = "../../../Data/Backup/";

    [Fact]
    public void FileSyncChecks(){
        var handler = new BackupHandler(new BlacklistBackupConfig(ORIGIN_PATH, BACKUP_PATH));
        var syncedFile = handler.GetFile("synced.txt");
        var changedFile = handler.GetFile("tosync.txt");
        var deletedFile = handler.GetFile("deleted.txt");
        //var areSame = syncedFile.IsSynced();
        Assert.True(syncedFile.IsSynced());
        Assert.False(changedFile.IsSynced());
        Assert.True(deletedFile.IsDeleted());
        Assert.True(syncedFile.CompareHashes());
        Assert.False(changedFile.CompareHashes());
    }
}
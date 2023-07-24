namespace IBS.Core.Test.Unit;

public sealed class FileSyncTests{
    public const string ORIGIN_PATH = "../../../Data/Origin/";
    public const string BACKUP_PATH = "../../../Data/Backup/";

    [Fact]
    public void FileSyncChecks(){
        var config = new BackupConfig(ORIGIN_PATH, BACKUP_PATH);
        var testFile = config.GetFile("test.txt");
        var areSame = testFile.IsSynced();
        Assert.True(areSame);
    }
}
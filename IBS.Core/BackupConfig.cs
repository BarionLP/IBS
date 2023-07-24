namespace IBS.Core;

public record BackupConfig(string Origin, string Backup){
    public BackedupFile GetFile(string relativePath){
        return new (Path.Combine(Origin, relativePath), Path.Combine(Backup, relativePath));
    }
}

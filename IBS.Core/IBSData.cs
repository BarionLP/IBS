namespace IBS.Core;

public static class IBSData{
    public static readonly DirectoryInfo DataDirectory = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IBS"));
    public static readonly DirectoryInfo LogsDirectory = new(Path.Combine(DataDirectory.FullName, "Logs"));
    public static readonly FileInfo DataFile = new(Path.Combine(DataDirectory.FullName, "Backups.txt"));

    public static void Init(){
        if (!DataDirectory.Exists) DataDirectory.Create();
        //if(!LogsDirectory.Exists) LogsDirectory.Create();
    }
}

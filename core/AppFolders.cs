using System;

namespace IBS.Core;

public static class AppFolders
{
    public static readonly DirectoryInfo DataDirectory = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IBS"));
    public static readonly DirectoryInfo LogsDirectory = new(Path.Combine(DataDirectory.FullName, "Logs"));
    public static readonly FileInfo DataFile = new(Path.Combine(DataDirectory.FullName, "backups.txt"));

    public static void Init()
    {
        DataDirectory.CreateIfNotExists();
    }
}
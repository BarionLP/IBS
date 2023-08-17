using IBS.Core;
using IBS.DataPersistence;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Diagnostics;

namespace IBS.GUI;

public partial class App : Application{
    const string BACKUP_PATH = @"I:\Coding\Tools\IBS\TestData\Backup";
    const string ORIGIN_PATH = @"I:\Coding\Tools\IBS\TestData\Origin";
    public static readonly DirectoryInfo DataDirectory = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IBS"));
    public static readonly DirectoryInfo LogsDirectory = new(Path.Combine(DataDirectory.FullName, "Logs"));
    public static readonly FileInfo DataFile = new(Path.Combine(DataDirectory.FullName, "Backups.txt"));

    public static event Action? OnBackupConfigsChange;

    public static List<IBackupConfig> BackupConfigs { get; } = new() {
        //new BlacklistBackupConfig(@"I:\Coding\Tools\IBS\TestData\Origin", @"I:\Coding\Tools\IBS\TestData\Backup"),
        //new BlacklistBackupConfig(@"D:\", @"B:\Backup\Data").IgnorePaths(@"D:\Util").IgnoreFolders(".dtrash"),
        //new BlacklistBackupConfig(@"I:\", @"B:\Backup\Projects")
            //.IgnorePaths(@"I:\AssetLibrary\SFX\DaVinci Resolve", @"I:\Movies\DaVinci Resolve", @"I:\SteamLibrary")
            //.IgnoreFolders(".git", ".vs", "bin", "Build", "Builds", "Library", "Logs", "obj", "publish", "Temp", "UserSettings", ".gradle", ".idea", "build"),
    };

    protected override void OnStartup(StartupEventArgs args) {
        base.OnStartup(args);

        if(!DataDirectory.Exists) DataDirectory.Create();
        //if(!LogsDirectory.Exists) LogsDirectory.Create();
        if(DataFile.Exists) {
            _ = LoadConfigs();
        }
    }

    public static async Task LoadConfigs() {
        //BackupConfigs.Clear();
        using var stream = DataFile.OpenText();

        while(await stream.ReadLineAsync() is string backup) {
            var fileInfo = new FileInfo(backup);
            if(!fileInfo.Exists) continue;
            (await BackupConfigExtensions.ReadAsync(fileInfo)).Resolve(BackupConfigs.Add, (error)=> Trace.TraceWarning("Failed Reading {0} with error {1}", fileInfo.FullName, error));
        }
        OnBackupConfigsChange?.Invoke();
    }
    
    public static async Task SaveConfigs() {
        using var stream = DataFile.CreateText();

        foreach(var config in BackupConfigs) {
            config.Save();
            await stream.WriteLineAsync(config.ConfigFileInfo.FullName);
        }
    }

    protected override void OnExit(ExitEventArgs args) {
        _ = SaveConfigs();

        base.OnExit(args);
    }
}

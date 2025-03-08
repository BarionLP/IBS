using IBS.Core.Serialization;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace IBS;

public partial class App : Application
{
    public static ObservableCollection<IBackupConfig> BackupConfigs { get; } = [];

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        IBSData.Init();
        if (IBSData.DataFile.Exists)
        {
            _ = LoadConfigs();
        }
    }

    public static async Task LoadConfigs(bool clearOld = true)
    {
        if (clearOld)
        {
            BackupConfigs.Clear();
        }

        using var stream = IBSData.DataFile.OpenText();

        while (await stream.ReadLineAsync() is string backup)
        {
            var fileInfo = new FileInfo(backup);
            if (!fileInfo.Exists)
                continue;
            (await BackupConfigExtensions.ReadAsync(fileInfo)).Consume(BackupConfigs.Add, () => Trace.TraceWarning("Failed Reading {0}", fileInfo.FullName));
        }
    }


    public static async Task SaveConfigs()
    {
        using var stream = IBSData.DataFile.CreateText();

        foreach (var config in BackupConfigs)
        {
            config.Save();
            await stream.WriteLineAsync(config.ConfigFileInfo.FullName);
        }
    }

    public static void AddBackupConfig(IBackupConfig config)
    {
        BackupConfigs.Add(config);
        _ = SaveConfigs();
    }
}


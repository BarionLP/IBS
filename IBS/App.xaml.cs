using System.Collections.ObjectModel;
using System.Diagnostics;
using Ametrin.Utils.Optional;
using IBS.Core;
using IBS.Core.Serialization;

namespace IBS;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new MainPage();
    }

    //public static event Action? OnBackupConfigsChange;
    public static ObservableCollection<IBackupConfig> BackupConfigs { get; } = new();

    protected override void OnStart()
    {
        base.OnStart();

        IBSData.Init();
        if (IBSData.DataFile.Exists)
        {
            _ = LoadConfigs();
        }
    }

    public static async Task LoadConfigs(bool clearOld = false)
    {
        if (clearOld)
            BackupConfigs.Clear();
        using var stream = IBSData.DataFile.OpenText();

        while (await stream.ReadLineAsync() is string backup)
        {
            var fileInfo = new FileInfo(backup);
            if (!fileInfo.Exists)
                continue;
            (await BackupConfigExtensions.ReadAsync(fileInfo)).Resolve(BackupConfigs.Add, () => Trace.TraceWarning("Failed Reading {0}", fileInfo.FullName));
        }
        //OnBackupConfigsChange?.Invoke();
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
        //OnBackupConfigsChange?.Invoke();
        _ = SaveConfigs();
    }
}

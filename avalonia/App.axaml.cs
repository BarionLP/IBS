using System.Collections.ObjectModel;
using Ametrin.Utils.Avalonia;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace IBS;

public sealed partial class App : Application
{
    public static ObservableCollection<BackupConfig> BackupConfigs { get; } = [];

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();

        AppFolders.Init();
        if (AppFolders.DataFile.Exists)
        {
            _ = LoadConfigs();
        }
        else
        {
            AppFolders.DataFile.Create();
        }
    }

    public static async Task LoadConfigs()
    {
        using var stream = AppFolders.DataFile.OpenText();

        while (await stream.ReadLineAsync() is string backup)
        {
            if (string.IsNullOrWhiteSpace(backup) || backup.StartsWith('#')) continue;
            var fileInfo = new FileInfo(backup);
            if (!fileInfo.Exists)
            {
                continue;
            }

            BackupConfigSerializer.Load(fileInfo).Consume(BackupConfigs.Add,
                e => MessageBox.Warning($"Failed Reading Backup Config\n{fileInfo.FullName}\n{e.Message}").Show(MessageBoxResult.Ignore)
            );
        }
    }

    public static async Task SaveConfigs()
    {
        using var stream = AppFolders.DataFile.CreateText();

        foreach (var config in BackupConfigs)
        {
            BackupConfigSerializer.Save(config);
            await stream.WriteLineAsync(config.ConfigFileInfo.FullName);
        }
    }

    public static void AddBackupConfig(BackupConfig config)
    {
        BackupConfigs.Add(config);
        _ = SaveConfigs();
    }
}
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Ametrin.Utils.WPF;

namespace IBS;

public partial class App : Application
{
    public static ObservableCollection<BackupConfig> BackupConfigs { get; } = [];

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppFolders.Init();
        if (AppFolders.DataFile.Exists)
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

        using var stream = AppFolders.DataFile.OpenText();

        while (await stream.ReadLineAsync() is string backup)
        {
            var fileInfo = new FileInfo(backup);
            if (!fileInfo.Exists)
            {
                continue;
            }

            BackupConfigSerializer.Load(fileInfo).Consume(BackupConfigs.Add, () => MessageBoxHelper.ShowWaring($"Failed Reading Backup Config:\n{fileInfo.FullName}"));
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

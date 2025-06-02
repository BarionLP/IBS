using Ametrin.Utils.WPF;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace IBS;

public partial class MainWindow : Window
{
    private readonly Progress<float> _progress;
    private readonly Progress<string> _workingOn;
    private BackupConfig? SelectedBackupConfig 
    { 
        get;
        set
        {
            field = value;
            SyncButton.IsEnabled = field is not null;
            VerifyButton.IsEnabled = field is not null;
        } 
    }
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        ResetProgress();
        BackupsView.ItemsSource = App.BackupConfigs;
        SelectedBackupConfig = null;

        _progress = new(value =>
        {
            ProgressDisplay.Value = value;
        });

        _workingOn = new Progress<string>(value =>
        {
            StatusLabel.Content = value;
        });
    }

    private async void Sync_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedBackupConfig is null)
        {
            return;
        }

        await TryAction("Syncing...", () => FileSyncer.AdvancedSync(SelectedBackupConfig, _progress, _workingOn));
    }

    private void Verify_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedBackupConfig is null)
        {
            return;
        }

        // await TryAction("Verifying...", () => BackupManager.VerifyBackup(_progress));
    }

    private async Task TryAction(string label, Action action)
    {
        ResetProgress();
        StatusLabel.Content = label;
        try
        {
            await Task.Run(action);
            FinishProgress();
        }
        catch (Exception ex)
        {
            MessageBoxHelper.ShowError(ex.Message, owner: this);
            StatusLabel.Content = "Failed!";
        }
    }

    private void AddBackupLocation(object sender, RoutedEventArgs e)
    {
        if (SelectedBackupConfig is null)
        {
            return;
        }

        var dialog = new Microsoft.Win32.OpenFolderDialog();
        if (dialog.ShowDialog() is true)
        {
            var result = dialog.FolderName;
            SelectedBackupConfig.AddBackupLocation(result);
            BackupConfigSerializer.Save(SelectedBackupConfig);
            BackupLocations.ItemsSource = null;
            BackupLocations.ItemsSource = SelectedBackupConfig.BackupDirectories;
        }
    }

    private void BackupSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BackupsView.SelectedItem is not BackupConfig selected)
        {
            SelectedBackupConfig = null;
            BackupLocations.ItemsSource = null;
            return;
        }

        SelectedBackupConfig = selected;
        BackupLocations.ItemsSource = SelectedBackupConfig.BackupDirectories;
    }

    private void AddBackupConfig(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog() { Title = "Pick Origin" };
        if (dialog.ShowDialog() is not true)
        {
            return;
        }

        var originPath = dialog.FolderName;

        dialog.Title = "Pick Backup Location";

        if (dialog.ShowDialog() is not true)
        {
            return;
        }
        var backupPath = dialog.FolderName;

        App.AddBackupConfig(BackupConfig.Create(originPath, backupPath));
    }

    private void ResetProgress()
    {
        ProgressDisplay.Value = 0;
    }
    private void FinishProgress()
    {
        ProgressDisplay.Value = 1;
        StatusLabel.Content = "Finished";
    }
}
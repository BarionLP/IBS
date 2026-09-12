using System.Diagnostics;
using Ametrin.Utils.Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace IBS;

public sealed partial class MainWindow : Window
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

        _workingOn = new(value =>
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

        await TryAction("Syncing...", () => FileSyncer.Sync(SelectedBackupConfig, _progress, _workingOn));
    }

    private void Verify_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedBackupConfig is null)
        {
            return;
        }

        // await TryAction("Verifying...", () => BackupManager.VerifyBackup(_progress));
    }

    private async Task TryAction(string label, Func<Task<ErrorState>> action)
    {
        ResetProgress();
        StatusLabel.Content = label;
        ProgressDisplay.IsIndeterminate = true;
        try
        {
            var error = await Task.Run(action);
            if (error.Branch(out var e))
            {
                FinishProgress();
            }
            else
            {
                await MessageBox.Error(e.Message).ShowDialog(this, MessageBoxResult.Ok);
                StatusLabel.Content = "Failed!";
            }
        }
        catch (Exception ex)
        {
            await MessageBox.Error(ex.Message).ShowDialog(this, MessageBoxResult.Ok);
            StatusLabel.Content = "Failed!";
        }
        ProgressDisplay.IsIndeterminate = false;
    }

    private async void AddBackupLocation(object sender, RoutedEventArgs e)
    {
        if (SelectedBackupConfig is null)
        {
            return;
        }

        var folders = await StorageProvider.OpenFolderPickerAsync(new());

        if (folders.Count is 0) return;

        var path = folders[0].TryGetLocalPath();

        Debug.Assert(path is not null);

        SelectedBackupConfig.AddBackupLocation(path);
        BackupConfigSerializer.Save(SelectedBackupConfig);

        BackupLocations.ItemsSource = null;
        BackupLocations.ItemsSource = SelectedBackupConfig.BackupDirectories;
        await (await BackupV2.CreateAsync(new(path))).SaveAsync();
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

    private async void AddBackupConfig(object sender, RoutedEventArgs e)
    {

        var folders = await StorageProvider.OpenFolderPickerAsync(new() { Title = "Pick Origin" });
        if (folders is not [var origin])
        {
            return;
        }

        var originPath = origin.TryGetLocalPath()!;

        folders = await StorageProvider.OpenFolderPickerAsync(new() { Title = "Pick Backup location" });


        if (folders is not [var backup])
        {
            return;
        }

        var backupPath = backup.TryGetLocalPath()!;

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
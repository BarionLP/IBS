using CommunityToolkit.Maui.Storage;
using IBS.Core;
using IBS.Core.Serialization;

namespace IBS;

public sealed partial class MainPage : ContentPage
{
    private readonly Progress<float> _progress;
    private readonly Progress<string> _workingOn;

    public MainPage()
    {
        InitializeComponent();
        ResetProgress();
        BackupsView.ItemsSource = App.BackupConfigs;
        //App.OnBackupConfigsChange += UpdateConfigSelection;
        _progress = new(value =>
        {
            ProgressDisplay.Progress = value;
        });
        _workingOn = new Progress<string>(value =>
        {
            StatusLabel.Text = value;
        });
    }

    private async void OnSync(object sender, EventArgs e)
    {
        if (BackupManager.Handler is null)
            return;

        ResetProgress();
        StatusLabel.Text = "Syncing...";
        await Task.Run(() => BackupManager.SyncBackup(_progress, _workingOn));
        FinishProgress();
    }

    private async void OnClean(object sender, EventArgs e)
    {
        if (BackupManager.Handler is null)
            return;

        ResetProgress();
        StatusLabel.Text = "Cleaning...";
        await Task.Run(() => BackupManager.CleanBackup(_progress, _workingOn));
        FinishProgress();
    }

    private async void OnVerify(object sender, EventArgs e)
    {
        if (BackupManager.Handler is null)
            return;

        ResetProgress();
        StatusLabel.Text = "Verifying...";
        await Task.Run(() => BackupManager.VerifyBackup(_progress));
        FinishProgress();
    }

    //public void UpdateConfigSelection(){
    //	BackupsView.ItemsSource = App.BackupConfigs;
    //}

    private void BackupSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BackupsView.SelectedItem is not BlacklistBackupConfig selected)
            return;
        BackupManager.SetConfig(new(selected));
        BackupLocations.ItemsSource = selected.BackupInfos;
    }

    private async void AddBackupLocation(object sender, EventArgs e)
    {
        if (BackupManager.Handler is null)
            return;

        var source = new CancellationTokenSource();
        var token = source.Token;
        var result = await FolderPicker.Default.PickAsync(token);

        if (!result.IsSuccessful)
            return;

        BackupManager.Handler.Config.AddBackupLocation(result.Folder.Path);
        BackupManager.Handler.Config.Save();
        BackupLocations.ItemsSource = null;
        BackupLocations.ItemsSource = BackupManager.Handler.Config.BackupInfos;
    }

    private async void AddLocation(object sender, EventArgs e)
    {
        var source = new CancellationTokenSource();
        var token = source.Token;

        var originFolder = await FolderPicker.Default.PickAsync(token);
        if (!originFolder.IsSuccessful)
            return;
        var backupFolder = await FolderPicker.Default.PickAsync(token);
        if (!backupFolder.IsSuccessful)
            return;

        App.AddBackupConfig(BlacklistBackupConfig.Create(originFolder.Folder.Path, backupFolder.Folder.Path));
    }


    private void ResetProgress()
    {
        ProgressDisplay.Progress = 0;
    }
    private void FinishProgress()
    {
        ProgressDisplay.Progress = 1;
        StatusLabel.Text = "Finished";
    }
}
using CommunityToolkit.Maui.Storage;
using IBS.Core;

namespace IBS;

public sealed partial class MainPage : ContentPage{
	public MainPage(){
		InitializeComponent();
		ResetProgress();
		App.OnBackupConfigsChange += UpdateConfigSelection;
	}

	private async void OnSync(object sender, EventArgs e){
		if (BackupManager.Handler is null) return;

		var progress = new Progress<(float progress, string file)>(value => {
			ProgressDisplay.Progress = value.progress;
			StatusLabel.Text = value.file;
		});
		ResetProgress(); 
		await Task.Run(() => BackupManager.SyncBackup(progress));
		FinishProgress();
	}

	private async void OnClean(object sender, EventArgs e){
		if (BackupManager.Handler is null) return;

		var progress = new Progress<float>(value => {
			ProgressDisplay.Progress = value;
		});
		ResetProgress();
		StatusLabel.Text = "Cleaning...";
		await Task.Run(() => BackupManager.CleanBackup(progress));
		FinishProgress();
	}

	private async void OnVerify(object sender, EventArgs e){
		if (BackupManager.Handler is null) return;

		var progress = new Progress<float>(value => {
			ProgressDisplay.Progress = value;
		});
		ResetProgress();
		StatusLabel.Text = "Verifing...";
		await Task.Run(() => BackupManager.VerifyBackup(progress));
		FinishProgress();
	}

	public void UpdateConfigSelection(){
		BackupsView.ItemsSource = App.BackupConfigs;
	}

	private void BackupSelectionChanged(object sender, SelectionChangedEventArgs e){
		if (BackupsView.SelectedItem is not BlacklistBackupConfig selected) return;
		BackupManager.SetConfig(new(selected));
		BackupLocations.ItemsSource = selected.BackupInfos;
	}

	private async void AddBackupLocation(object sender, EventArgs e){
		if (BackupManager.Handler is null) return;

		var source = new CancellationTokenSource();
		var token = source.Token;
		var result = await FolderPicker.Default.PickAsync(token);

		if (!result.IsSuccessful) return;

		BackupManager.Handler.Config.AddBackupLocation(result.Folder.Path);
		BackupLocations.ItemsSource = null;
		BackupLocations.ItemsSource = BackupManager.Handler.Config.BackupInfos;
	}

	private async void AddLocation(object sender, EventArgs e){	
		var source = new CancellationTokenSource();
		var token = source.Token;
		var originFolder = await FolderPicker.Default.PickAsync(token);
		if (!originFolder.IsSuccessful) return;
		var backupFolder = await FolderPicker.Default.PickAsync(token);
		if (!backupFolder.IsSuccessful) return;
		
		App.AddBackupConfig(new BlacklistBackupConfig(originFolder.Folder.Path, backupFolder.Folder.Path));
	}


	private void ResetProgress(){
		ProgressDisplay.Progress = 0;
	}
	private void FinishProgress(){
		ProgressDisplay.Progress = 1;
		StatusLabel.Text = "Finished";
	}
}
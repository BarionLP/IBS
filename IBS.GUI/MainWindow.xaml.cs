using IBS.Core;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace IBS.GUI;

public partial class MainWindow : Window{
    public MainWindow(){
        InitializeComponent();
        BackupsView.SelectedIndex = 0;
        App.OnBackupConfigsChange += UpdateConfigSelection;
    }

    private async void SyncClicked(object sender, RoutedEventArgs args){
        var progress = new Progress<(float progress, string file)>(value => {
            SyncProgressBar.Value = value.progress;
            FileNameDisplay.Text = value.file;
        });
        SyncProgressBar.Value = 0;
        await Task.Run(()=> BackupManager.SyncBackup(progress));
        FileNameDisplay.Text = "finished";
    }

    private void BackupSelectionChanged(object sender, SelectionChangedEventArgs args) {
        if(BackupsView.SelectedItem is not BlacklistBackupConfig selected) return;
        BackupManager.SetConfig(new(selected));
    }

    private async void CleanClicked(object sender, RoutedEventArgs args) {
        var progress = new Progress<float>(value => {
            SyncProgressBar.Value = value;
        });
        SyncProgressBar.Value = 0;
        FileNameDisplay.Text = "Cleaning...";
        await Task.Run(() => BackupManager.CleanBackup(progress));
        FileNameDisplay.Text = "finished";
    }

    public void UpdateConfigSelection() {
        BackupsView.ItemsSource = App.BackupConfigs;
    }
}

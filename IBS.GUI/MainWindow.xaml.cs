using IBS.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

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
        BackupLocations.ItemsSource = selected.BackupInfos;
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
    private async void VerifyClicked(object sender, RoutedEventArgs args) {
        var progress = new Progress<float>(value => {
            SyncProgressBar.Value = value;
        });
        SyncProgressBar.Value = 0;
        FileNameDisplay.Text = "Verifing...";
        await Task.Run(() => BackupManager.VerifyBackup(progress));
        FileNameDisplay.Text = "finished";
    }

    public void UpdateConfigSelection() {
        BackupsView.ItemsSource = App.BackupConfigs;
    }

    private void AddBackupLocation(object sender, RoutedEventArgs args) {
        if(BackupManager.Handler is null) return;
        
        using var dialog = new FolderBrowserDialog();

        if(dialog.ShowDialog() is not System.Windows.Forms.DialogResult.OK) return;

        BackupManager.Handler.Config.AddBackupLocation(dialog.SelectedPath);
        BackupLocations.ItemsSource = null;
        BackupLocations.ItemsSource = BackupManager.Handler.Config.BackupInfos;
    }
}

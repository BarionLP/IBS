using Ametrin.Utils;
using System.Diagnostics;
using IBS.Core;
using IBS.Core.Serialization;

namespace IBS;

public partial class App : Application{
	public static event Action? OnBackupConfigsChange;
	public static List<IBackupConfig> BackupConfigs { get; } = new();
	
	public App(){
		InitializeComponent();
		MainPage = new MainPage();

		IBSData.Init();
		if (IBSData.DataFile.Exists){
			_ = Init();
		}
	}

	public static async Task Init(){
		Trace.TraceInformation("inited");
		BackupConfigs.AddRange(await BackupConfigExtensions.LoadConfigs());
		OnBackupConfigsChange?.Invoke();
		Trace.TraceInformation(BackupConfigs.Select(config=> config.OriginInfo.FullName).Dump(", "));
	}


	public static async Task SaveConfigs(){
		using var stream = IBSData.DataFile.CreateText();

		foreach (var config in BackupConfigs){
			config.Save();
			await stream.WriteLineAsync(config.ConfigFileInfo.FullName);
		}
	}

	public static void AddBackupConfig(IBackupConfig config){
		BackupConfigs.Add(config);
		OnBackupConfigsChange?.Invoke();
		_ = SaveConfigs();
	}
}

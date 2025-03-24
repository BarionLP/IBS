using IBS.Core;
using IBS.Core.Serialization;

// var config = BackupConfig.Create("Data/Origin", "Data/Backup");
var config = BackupConfigSerializer.Load(new ("Data/Origin/backup_config.json"));

FileSyncer.AdvancedSync(config);
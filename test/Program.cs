using IBS.Core;
using IBS.Core.Serialization;

var config = BackupConfig.Create("Data/Origin", "Data/Backup");

BackupConfigSerializer.Save(config);
FileSyncer.AdvancedSync(config);
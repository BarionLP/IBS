using Ametrin.Serialization;

namespace IBS.Core.Serialization;

public static class BackupConfigSerializer
{
    public static void Save(BackupConfig config) => config.WriteToJsonFile(config.ConfigFileInfo);
    public static BackupConfig Load(FileInfo fileInfo) => JsonExtensions.ReadFromJsonFile<BackupConfig>(fileInfo).OrThrow();
}
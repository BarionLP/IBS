using Ametrin.Serialization;

namespace IBS.Core.Serialization;

public static class BackupConfigSerializer
{
    public static void Save(BackupConfig config) => config.WriteToJsonFile(config.ConfigFileInfo);
    public static Result<BackupConfig> Load(FileInfo fileInfo)
    {
        var config = JsonExtensions.ReadFromJsonFile(fileInfo, BackupJsonContext.Default.BackupConfig);

        config.Consume(static config =>
        {
            foreach (var i in ..config.BackupDirectories.Count)
            {
                if (config.BackupDirectories[i].Name is "_Storage")
                {
                    config.BackupDirectories[i] = config.BackupDirectories[i].Parent!;
                }
            }
        });
        
        return config;
    }
}
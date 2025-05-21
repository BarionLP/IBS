using Ametrin.Serialization;

namespace IBS.Core.Serialization;

public static class BackupConfigSerializer
{
    public static void Save(BackupConfig config) => config.WriteToJsonFile(config.ConfigFileInfo);
    public static Result<BackupConfig> Load(FileInfo fileInfo)
    {
        var config = JsonExtensions.ReadFromJsonFile<BackupConfig>(fileInfo);

        config.Consume(static config =>
        {
            foreach (var i in ..config.BackupInfos.Count)
            {
                if (config.BackupInfos[i].Name is "_Storage")
                {
                    config.BackupInfos[i] = config.BackupInfos[i].Parent;
                }
            }
        });
        
        return config;
    }
}
using Ametrin.Serialization;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IBS.Core.Serialization;

public static class BackupConfigExtensions
{
    private static readonly Dictionary<string, Type> TypeRegistry = new();

    static BackupConfigExtensions()
    {
        TypeRegistry.TryAdd<BlacklistBackupConfig>();
    }

    public static async Task<List<IBackupConfig>> LoadConfigs()
    {
        using var stream = IBSData.DataFile.OpenText();
        var result = new List<IBackupConfig>(4);

        while (await stream.ReadLineAsync() is string backup)
        {
            var fileInfo = new FileInfo(backup);
            if (!fileInfo.Exists)
                continue;
            (await ReadAsync(fileInfo)).Consume(result.Add, () => Trace.TraceWarning("Failed Reading {0}", fileInfo.FullName));
        }

        return result;
    }

    public static void Save<T>(this T config) where T : IBackupConfig
    {
        var typeName = config.GetType().FullName!;
        if (!TypeRegistry.ContainsKey(typeName))
            throw new ArgumentException($"{typeName} is not supported");
        try
        {
            var fo = new BackupConfigFile(typeName, config.ConvertToJsonElement());
            fo.WriteToJsonFile(config.ConfigFileInfo);
        }
        catch (Exception e)
        {
            Trace.TraceError(e.Message);
        }
    }
    public static Task<Option<IBackupConfig>> ReadAsync(FileInfo targetFile)
    {
        return Task.Run(() => JsonExtensions.ReadFromJsonFile<BackupConfigFile>(targetFile)
            .Map(fo => TypeRegistry.TryGetValue(fo.TypeID)
            .Map(type => fo.Body.Deserialize(type, JsonExtensions.DefaultOptions).ToOption<IBackupConfig>())));
    }

    private sealed class BackupConfigFile(string typeId, JsonElement body)
    {
        [JsonInclude] public string TypeID { get; private set; } = typeId;
        [JsonInclude] public JsonElement Body { get; private set; } = body;
    }
}

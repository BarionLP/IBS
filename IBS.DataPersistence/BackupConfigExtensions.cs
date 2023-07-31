using Ametrin.Serialization;
using Ametrin.Utils;
using IBS.Core;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IBS.DataPersistence;
public static class BackupConfigExtensions {
    private static readonly IReadOnlyDictionary<string, Type> TypeRegistry = new Dictionary<string, Type>() {
        {typeof(BlacklistBackupConfig).FullName!, typeof(BlacklistBackupConfig)},
    }.AsReadOnly();

    public static void Save<T>(this T config) where T : IBackupConfig {
        var typeName = config.GetType().FullName!;
        if(!TypeRegistry.ContainsKey(typeName)) throw new ArgumentException($"{typeName} is not supported");
        try {
            var fo = new BackupConfigFile(typeName, config.ConvertToJsonElement());
            fo.WriteToJsonFile(config.ConfigFileInfo);
        } catch(Exception e) {
            Trace.TraceError(e.Message);
        }
    }
    public static async Task<Result<IBackupConfig>> ReadAsync(FileInfo targetFile) {
        try {
            if(await JsonExtensions.ReadFromJsonFileAsync<BackupConfigFile>(targetFile) is not BackupConfigFile fo) return ResultStatus.ResultNull;

            if(!TypeRegistry.TryGetValue(fo.TypeID, out var type)) return ResultStatus.InvalidArgument;
            if(JsonSerializer.Deserialize(fo.Body, type) is not IBackupConfig config) return ResultStatus.Failed;
            //config.ConfigFileInfo = targetFile;
            return Result<IBackupConfig>.Succeeded(config);
        } catch(Exception e) {
            Trace.TraceWarning(e.Message);
            return ResultStatus.Failed;
        }
    }

    private sealed class BackupConfigFile {
        [JsonInclude] public string TypeID { get; private set; }
        [JsonInclude] public JsonElement Body { get; private set; }

        internal BackupConfigFile(string typeId, JsonElement body) {
            TypeID = typeId;
            Body = body;
        }

        public BackupConfigFile() { }
    }
}

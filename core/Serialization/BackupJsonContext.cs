using System.Text.Json.Serialization;
using Ametrin.Serialization;

namespace IBS.Core.Serialization;

[JsonSerializable(typeof(BackupMetaData), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(BackupConfig), GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSerializable(typeof(Dictionary<string, DateTime>), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSourceGenerationOptions(Converters = [typeof(FileInfoJsonConverter), typeof(DirectoryInfoJsonConverter)], WriteIndented = true, AllowTrailingCommas = true, ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip)]
internal partial class BackupJsonContext : JsonSerializerContext;
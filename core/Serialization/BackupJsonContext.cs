using System.Text.Json;
using System.Text.Json.Serialization;
using Ametrin.Serialization;

namespace IBS.Core.Serialization;

[JsonSerializable(typeof(BackupMetaData), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(BackupConfig), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Dictionary<string, DateTime>), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(BackupV2.DirectoryNodeInfo), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSourceGenerationOptions(Converters = [typeof(FileInfoJsonConverter), typeof(DirectoryInfoJsonConverter)], WriteIndented = true, AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class BackupJsonContext : JsonSerializerContext;
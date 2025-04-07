// using System.Text.Json;
// using System.Text.Json.Serialization;

// namespace IBS.Core.Serialization;

// public sealed class BackupInfoJsonConverter : JsonConverter<Backup>
// {
//     public override Backup Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//     {
//         var path = reader.GetString() ?? throw new JsonException("Invalid");
//         if (path.EndsWith("_Storage")) // legacy
//         {
//             path = path[..^8];
//         }
//         return new Backup(new(path));
//     }

//     public override void Write(Utf8JsonWriter writer, Backup value, JsonSerializerOptions options)
//     {
//         writer.WriteStringValue(value.Root.FullName);
//     }
// }
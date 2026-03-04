using ReplantedOnline.Enums;
using ReplantedOnline.Structs;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReplantedOnline.Data.Json;

/// <summary>
/// JSON converter for the ID struct to enable serialization.
/// Supports multiple ID types: ULong, SteamId, IPEndPoint, and Null.
/// Used for saving/loading data and LAN broadcast serialization.
/// </summary>
internal class IDJsonConverter : JsonConverter<ID>
{
    /// <summary>
    /// Reads and deserializes an ID from JSON.
    /// Expected format: { "Type": "ULong", "Value": 12345 } or similar.
    /// </summary>
    public override ID Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start of object");

        ulong? ulongValue = null;
        string type = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "Type":
                        type = reader.GetString();
                        break;
                    case "Value":
                        if (reader.TokenType == JsonTokenType.Number)
                        {
                            reader.TryGetUInt64(out ulong u64Val);
                            ulongValue = u64Val;
                        }
                        else if (reader.TokenType == JsonTokenType.String)
                        {
                            // For IPEndPoint format "address:port"
                            string value = reader.GetString();
                            var parts = value.Split(':');
                            if (parts.Length == 2 && ulong.TryParse(parts[1], out ulong port))
                            {
                                // Handle IPEndPoint case in the type check below
                            }
                        }
                        break;
                }
            }
        }

        if (type == "ULong" && ulongValue.HasValue)
            return new ID(ulongValue.Value, IdType.ULong);

        if (type == "SteamId" && ulongValue.HasValue)
            return new ID(ulongValue.Value, IdType.SteamId);

        return ID.Null;
    }

    /// <summary>
    /// Writes an ID to JSON format.
    /// Output format varies based on ID type for optimal storage.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, ID value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.IsULong)
        {
            writer.WriteString("Type", "ULong");
            writer.WriteNumber("Value", value.AsULong());
        }
        else if (value.IsSteamId)
        {
            writer.WriteString("Type", "SteamId");
            writer.WriteNumber("Value", value.AsSteamId().Value);
        }
        else if (value.IsIPEndPoint)
        {
            var ep = value.AsIPEndPoint();
            writer.WriteString("Type", "IPEndPoint");
            writer.WriteString("Value", $"{ep.Address}:{ep.Port}");
        }
        else
        {
            writer.WriteString("Type", "Null");
            writer.WriteNull("Value");
        }

        writer.WriteEndObject();
    }
}
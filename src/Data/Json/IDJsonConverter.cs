using ReplantedOnline.Enums;
using ReplantedOnline.Structs;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReplantedOnline.Data.Json;

/// <summary>
/// JSON converter for the ID struct to enable serialization
/// </summary>
internal class IDJsonConverter : JsonConverter<ID>
{
    public override ID Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

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
                        break;
                }
            }
        }

        if (type == "ULong" && ulongValue.HasValue)
            return new ID(ulongValue.Value, IdType.ULong);

        return ID.Null;
    }

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
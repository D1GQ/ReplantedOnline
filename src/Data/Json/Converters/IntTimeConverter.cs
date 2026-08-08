using ReplantedOnline.Structs;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReplantedOnline.Data.Json.Converters;

/// <summary>
/// Custom JSON converter for IntTime that serializes as seconds and deserializes back to IntTime.
/// </summary>
internal class IntTimeConverter : JsonConverter<IntTime>
{
    /// <summary>
    /// Reads an IntTime from JSON, expecting a number representing seconds.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The parsed IntTime.</returns>
    /// <exception cref="JsonException">Thrown when the value cannot be converted to IntTime.</exception>
    public override IntTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            float seconds = reader.GetSingle();
            return new IntTime(seconds);
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            string? value = reader.GetString();
            if (value != null && float.TryParse(value, out float seconds))
            {
                return new IntTime(seconds);
            }

            throw new JsonException($"Unable to convert '{value}' to IntTime. Expected a number representing seconds.");
        }

        throw new JsonException($"Unexpected token type '{reader.TokenType}' when parsing IntTime. Expected a number.");
    }

    /// <summary>
    /// Writes an IntTime to JSON as a float.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The IntTime to write.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, IntTime value, JsonSerializerOptions options)
    {
        float seconds = (float)value;
        writer.WriteNumberValue(seconds);
    }
}
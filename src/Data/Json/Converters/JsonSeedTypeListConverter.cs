using Il2CppReloaded.Gameplay;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReplantedOnline.Data.Json.Converters;

/// <summary>
/// Custom JSON converter for List of SeedType that builds off the existing SeedType converter.
/// </summary>
internal class JsonSeedTypeListConverter : JsonConverter<List<SeedType>>
{
    private static readonly JsonSeedTypeConverter _seedTypeConverter = new();

    /// <summary>
    /// Reads a List of SeedType from JSON, supporting both string names and numeric values.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The parsed List of SeedType.</returns>
    /// <exception cref="JsonException">Thrown when the array format is invalid or a value cannot be converted.</exception>
    public override List<SeedType> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Expected start of array, got '{reader.TokenType}'.");
        }

        var result = new List<SeedType>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return result;
            }

            // Reuse the existing converter for each item
            SeedType seedType = _seedTypeConverter.Read(ref reader, typeof(SeedType), options);
            result.Add(seedType);
        }

        throw new JsonException("Unexpected end of JSON when parsing SeedType array.");
    }

    /// <summary>
    /// Writes a List of SeedType to JSON, using custom names for custom seed types and enum names for regular ones.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The List of SeedType to write.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, List<SeedType> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        foreach (var seedType in value)
        {
            // Reuse the existing converter for each item
            _seedTypeConverter.Write(writer, seedType, options);
        }

        writer.WriteEndArray();
    }
}
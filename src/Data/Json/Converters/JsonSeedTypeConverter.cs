using Il2CppReloaded.Gameplay;
using ReplantedOnline.Structs.Reloaded;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReplantedOnline.Data.Json.Converters;

/// <summary>
/// Custom JSON converter for SeedType that handles custom seed type names.
/// </summary>
internal class JsonSeedTypeConverter : JsonConverter<SeedType>
{
    private static readonly Dictionary<string, SeedType> _customSeedTypeMap = [];
    private static readonly Dictionary<SeedType, string> _customSeedTypeReverseMap = [];
    private static readonly HashSet<SeedType> _customSeedTypeValues = [];

    /// <summary>
    /// Initializes the static mappings for custom seed types by reflecting over the <see cref="CustomSeedType"/> struct.
    /// </summary>
    static JsonSeedTypeConverter()
    {
        var properties = typeof(CustomSeedType).GetProperties(BindingFlags.NonPublic | BindingFlags.Static);

        foreach (var property in properties)
        {
            var value = property.GetValue(null);
            if (value is CustomSeedType customSeedType)
            {
                SeedType seedType = (SeedType)customSeedType;

                _customSeedTypeMap[property.Name] = seedType;
                _customSeedTypeReverseMap[seedType] = property.Name;
                _customSeedTypeValues.Add(seedType);
            }
        }
    }

    /// <summary>
    /// Reads a SeedType from JSON, supporting both string names and numeric values.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The parsed SeedType.</returns>
    /// <exception cref="JsonException">Thrown when the value cannot be converted to SeedType.</exception>
    public override SeedType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? value = reader.GetString();

            if (value != null && _customSeedTypeMap.TryGetValue(value, out SeedType seedType))
            {
                return seedType;
            }

            if (Enum.TryParse(value, ignoreCase: true, out SeedType parsedType))
            {
                return parsedType;
            }

            throw new JsonException($"Unable to convert '{value}' to SeedType.");
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            int numericValue = reader.GetInt32();
            return (SeedType)numericValue;
        }

        throw new JsonException($"Unexpected token type '{reader.TokenType}' when parsing SeedType.");
    }

    /// <summary>
    /// Writes a SeedType to JSON, using custom names for custom seed types and enum names for regular ones.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The SeedType to write.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, SeedType value, JsonSerializerOptions options)
    {
        if (_customSeedTypeValues.Contains(value) && _customSeedTypeReverseMap.TryGetValue(value, out string? customName))
        {
            writer.WriteStringValue(customName);
        }
        else
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
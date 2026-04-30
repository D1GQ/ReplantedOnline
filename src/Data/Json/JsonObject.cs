using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReplantedOnline.Data.Json;

/// <summary>
/// Provides a base class for JSON serializable objects with shared JSON serializer options.
/// </summary>
internal abstract class JsonObject
{
    /// <summary>
    /// Shared JSON serializer options for consistent serialization behavior across all derived types.
    /// </summary>
    protected static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };
}

/// <summary>
/// Provides a generic base class for JSON serializable objects with serialization and deserialization functionality.
/// </summary>
/// <typeparam name="T">The type of the JSON object that derives from this class.</typeparam>
internal abstract class JsonObject<T> : JsonObject where T : JsonObject<T>
{
    /// <summary>
    /// Serializes the specified object to a JSON string.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>A JSON string representation of the object, or an empty string if the object is null.</returns>
    internal static string Serialize(T obj)
    {
        if (obj == null)
            return string.Empty;

        return JsonSerializer.Serialize(obj, _serializerOptions);
    }

    /// <summary>
    /// Deserializes the specified JSON string to an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A deserialized object of type <typeparamref name="T"/>, or null if deserialization fails or the input is invalid.</returns>
    internal static T Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(json, _serializerOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Serializes the current instance to a JSON string.
    /// </summary>
    /// <returns>A JSON string representation of the current instance.</returns>
    internal string Serialize()
    {
        return Serialize((T)this);
    }
}
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReplantedOnline.Data.Json;

/// <summary>
/// Provides a base class for JSON serializable objects with shared JSON serializer options.
/// </summary>
internal abstract class JsonObject
{
    /// <summary>
    /// Gets the JSON serializer options for consistent serialization behavior across all derived types.
    /// </summary>
    protected virtual JsonSerializerOptions SerializerOptions
    {
        get
        {
            field ??= new()
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never
            };

            return field;
        }
    }
}

/// <summary>
/// Provides a generic base class for JSON serializable objects with serialization and deserialization functionality.
/// </summary>
/// <typeparam name="T">The type of the JSON object that derives from this class.</typeparam>
internal abstract class JsonObject<T> : JsonObject where T : JsonObject<T>
{
    private static readonly T UninitializedObject = (T)FormatterServices.GetUninitializedObject(typeof(T));

    /// <summary>
    /// Serializes the specified object to a JSON string using its instance Serialize method.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>A JSON string representation of the object.</returns>
    internal static string SerializeObject(T obj)
    {
        return obj.Serialize();
    }

    /// <summary>
    /// Deserializes the specified JSON string to an object of type <typeparamref name="T"/> 
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A deserialized object of type <typeparamref name="T"/>, or null if deserialization fails or the input is invalid.</returns>
    internal static T DeserializeObject(string json)
    {
        return UninitializedObject.Deserialize(json);
    }

    /// <summary>
    /// Serializes the current instance to a JSON string.
    /// </summary>
    /// <returns>A JSON string representation of the current instance.</returns>
    internal virtual string Serialize()
    {
        return JsonSerializer.Serialize(this, GetType(), SerializerOptions);
    }

    /// <summary>
    /// Deserializes the specified JSON string to an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A deserialized object of type <typeparamref name="T"/>, or null if deserialization fails or the input is invalid.</returns>
    internal virtual T Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(json, SerializerOptions);
        }
        catch
        {
            return null;
        }
    }
}
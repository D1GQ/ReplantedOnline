using ReplantedOnline.Utilities.MelonLoader;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReplantedOnline.Data.Json;

/// <summary>
/// Provides a base class for JSON serializable objects with configurable serializer options.
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

    /// <summary>
    /// Called before the object is serialized to JSON.
    /// </summary>
    protected virtual void OnSerialize() { }

    /// <summary>
    /// Called after the object has been deserialized from JSON.
    /// </summary>
    protected virtual void OnDeserialize() { }

    /// <summary>
    /// Serializes the current instance to a JSON string.
    /// </summary>
    /// <returns>A JSON string representation of the current instance.</returns>
    internal string Serialize()
    {
        OnSerialize();
        return JsonSerializer.Serialize(this, GetType(), SerializerOptions);
    }

    /// <summary>
    /// Deserializes the specified JSON string into the current instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    internal void Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            var newObj = (JsonObject?)Activator.CreateInstance(GetType())!;
            if (CopyProperties(newObj))
            {
                OnDeserialize();
            }

            return;
        }

        try
        {
            var obj = (JsonObject?)JsonSerializer.Deserialize(json, GetType(), SerializerOptions);
            if (obj != null)
            {
                if (CopyProperties(obj))
                {
                    OnDeserialize();
                }
            }
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(typeof(JsonObject), ex.ToString());
        }
    }

    /// <summary>
    /// Copies all public writable properties from one JsonObject to another.
    /// </summary>
    /// <param name="from">The source object to copy properties from.</param>
    /// <returns>
    /// <see langword="true"/> if the types match and properties were copied successfully;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private bool CopyProperties(JsonObject from)
    {
        var type = from.GetType();
        if (type != GetType())
            return false;

        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty);
        foreach (var property in properties)
        {
            if (!property.CanWrite)
                continue;

            property.SetValue(this, property.GetValue(from));
        }

        return true;
    }
}
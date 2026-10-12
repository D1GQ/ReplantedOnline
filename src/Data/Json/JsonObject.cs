using ReplantedOnline.Interfaces;
using ReplantedOnline.Utilities.MelonLoader;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReplantedOnline.Data.Json;

/// <summary>
/// Provides a base class for JSON serializable objects with configurable serializer options.
/// </summary>
internal abstract class JsonObject : ICatchedType
{
    /// <inheritdoc/>
    public Type CatchedType
    {
        get
        {
            if (field == null)
            {
                field = GetType();
            }

            return field;
        }
    }

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
        return JsonSerializer.Serialize(this, CatchedType, SerializerOptions);
    }

    /// <summary>
    /// Deserializes the specified JSON string into the current instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    internal void Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            var newObj = (JsonObject?)Activator.CreateInstance(CatchedType)!;
            if (CopyProperties(newObj))
            {
                OnDeserialize();
            }

            return;
        }

        try
        {
            var obj = (JsonObject?)JsonSerializer.Deserialize(json, CatchedType, SerializerOptions);
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
        var type = from.CatchedType;
        if (type != CatchedType)
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
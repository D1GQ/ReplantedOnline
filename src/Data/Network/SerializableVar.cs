using System.Globalization;

namespace ReplantedOnline.Data.Network;

/// <summary>
/// Provides base functionality for serializing and deserializing values for network transmission.
/// </summary>
/// <typeparam name="T">The type of the serializable value. Must be a non-nullable type.</typeparam>
internal abstract class SerializableVar<T> where T : notnull
{
    /// <summary>
    /// Gets the default value used when deserialization fails or data is empty.
    /// </summary>
    protected T DefaultValue { get; init; } = default!;

    /// <summary>
    /// Serializes a value into a string suitable for network transmission.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="type">The runtime type of the value.</param>
    /// <returns>A culture-invariant string representation of the value.</returns>
    protected static string Serialize(object value, Type type)
    {
        if (value == null)
            return string.Empty;

        if (type == typeof(string))
            return (string)value;

        if (type == typeof(bool))
            return ((bool)value).ToString();

        if (type.IsEnum)
            return Convert.ToUInt64(value, CultureInfo.InvariantCulture).ToString();

        if (value is IFormattable formattable)
            return formattable.ToString(null, CultureInfo.InvariantCulture);

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var itemType = type.GetGenericArguments()[0];
            var list = (System.Collections.IList)value;
            var serializedItems = new List<string>();

            foreach (var item in list)
            {
                serializedItems.Add(Serialize(item, itemType));
            }

            return string.Join("|", serializedItems);
        }

        throw new NotSupportedException($"Type {type} is not supported for serialization");
    }

    /// <summary>
    /// Deserializes a string value into the specified type.
    /// </summary>
    /// <param name="value">The string value received from the network.</param>
    /// <param name="type">The target type to convert the value into.</param>
    /// <returns>
    /// An instance of <paramref name="type"/> representing the deserialized value,
    /// or the <see cref="DefaultValue"/> if parsing fails.
    /// </returns>
    protected object Deserialize(string value, Type type)
    {
        if (string.IsNullOrEmpty(value))
            return DefaultValue;

        if (type == typeof(string))
            return value;

        if (type == typeof(bool))
            return bool.TryParse(value, out var b) ? b : DefaultValue;

        if (type.IsEnum)
        {
            if (ulong.TryParse(value, out var num))
                return Enum.ToObject(type, num);

            return DefaultValue;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var itemType = type.GetGenericArguments()[0];
            var items = value.Split(['|'], StringSplitOptions.None);
            var listType = typeof(List<>).MakeGenericType(itemType);
            var list = (System.Collections.IList)Activator.CreateInstance(listType)!;

            foreach (var item in items)
            {
                var deserializedItem = Deserialize(item, itemType);
                list.Add(deserializedItem);
            }

            return list;
        }

        try
        {
            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }
        catch
        {
            return DefaultValue;
        }
    }
}
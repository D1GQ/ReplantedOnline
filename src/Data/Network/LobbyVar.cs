using ReplantedOnline.Network.Client;
using System.Globalization;

namespace ReplantedOnline.Data.Network;

/// <summary>
/// Represents a synchronized lobby variable shared between all lobby members.
/// Only the lobby host can modify the value, and updates are automatically propagated to all clients.
/// </summary>
/// <typeparam name="T">The type of the lobby variable. Supported types include: <see cref="string"/>, <see cref="bool"/>, numeric primitives, and <see langword="enum"/> types.</typeparam>
internal sealed class LobbyVar<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LobbyVar{T}"/> class with a default value.
    /// </summary>
    /// <param name="varName">The unique key used to identify this variable in the lobby data store.</param>
    /// <param name="defaultValue">The default value returned when no valid data is available or deserialization fails.</param>
    internal LobbyVar(string varName, T defaultValue)
    {
        _varName = "lobby_var:" + varName.ToLower().Replace(' ', '_').Replace("_", "");
        _defaultValue = defaultValue;
    }

    private readonly string _varName;
    private readonly T _defaultValue;

    /// <summary>
    /// Gets or sets the current value of the lobby variable.
    /// </summary>
    internal T Value
    {
        get
        {
            return (T)Deserialize(
                ReplantedLobby.NetworkTransport.GetLobbyData(
                    ReplantedLobby.LobbyData.LobbyId,
                    _varName),
                typeof(T));
        }
        set
        {
            if (ReplantedLobby.AmLobbyHost())
            {
                ReplantedLobby.NetworkTransport.SetLobbyData(
                    ReplantedLobby.LobbyData.LobbyId,
                    _varName,
                    Serialize(value, typeof(T)));

                ReplantedLobby.LobbyData.UpdateLobbyStates();
            }
        }
    }

    /// <summary>
    /// Serializes a value into a string suitable for network transmission.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="type">The runtime type of the value.</param>
    /// <returns>A culture-invariant string representation of the value.</returns>
    /// <exception cref="NotSupportedException">Thrown if the provided type is not supported for serialization.</exception>
    private static string Serialize(object value, Type type)
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

        throw new NotSupportedException($"Type {type} is not supported for serialization");
    }

    /// <summary>
    /// Deserializes a string value into the specified type.
    /// </summary>
    /// <param name="value">The string value received from the network.</param>
    /// <param name="type">The target type to convert the value into.</param>
    /// <returns>
    /// An instance of <paramref name="type"/> representing the deserialized value,
    /// or the default value if parsing fails.
    /// </returns>
    private object Deserialize(string value, Type type)
    {
        if (string.IsNullOrEmpty(value))
            return _defaultValue;

        if (type == typeof(string))
            return value;

        if (type == typeof(bool))
            return bool.TryParse(value, out var b) ? b : _defaultValue;

        if (type.IsEnum)
        {
            if (ulong.TryParse(value, out var num))
                return Enum.ToObject(type, num);

            return _defaultValue;
        }

        try
        {
            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }
        catch
        {
            return _defaultValue;
        }
    }
}
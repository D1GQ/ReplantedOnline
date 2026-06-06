using ReplantedOnline.Network.Client;

namespace ReplantedOnline.Data.Network;

/// <summary>
/// Represents a synchronized lobby variable shared between all lobby members.
/// </summary>
/// <typeparam name="T">The type of the lobby variable.</typeparam>
internal sealed class LobbyVar<T> : SerializableVar<T> where T : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LobbyVar{T}"/> class with a default value.
    /// </summary>
    /// <param name="varName">The unique key used to identify this variable in the lobby data store. Spaces are converted to underscores.</param>
    /// <param name="defaultValue">The default value returned when no valid data is available or deserialization fails.</param>
    internal LobbyVar(string varName, T defaultValue)
    {
        _varName = "lobby_var:" + varName.ToLower().Replace(' ', '_').Replace("_", "");
        DefaultValue = defaultValue;
    }

    private readonly string _varName;

    /// <summary>
    /// Gets or sets the current value of the lobby variable.
    /// </summary>
    internal T Value
    {
        get
        {
            return (T)Deserialize(
                ReloadedLobby.NetworkTransport!.GetLobbyData(
                    ReloadedLobby.LobbyData!.LobbyId,
                    _varName),
                typeof(T));
        }
        set
        {
            if (ReloadedLobby.AmLobbyHost())
            {
                ReloadedLobby.NetworkTransport!.SetLobbyData(
                    ReloadedLobby.LobbyData!.LobbyId,
                    _varName,
                    Serialize(value, typeof(T)));

                ReloadedLobby.LobbyData.UpdateLobbyStates();
            }
        }
    }
}
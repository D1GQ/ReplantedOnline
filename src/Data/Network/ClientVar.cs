using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Structs.Network;

namespace ReplantedOnline.Data.Network;

/// <summary>
/// Represents a synchronized client-owned variable shared between lobby members.
/// </summary>
/// <typeparam name="T">The type of the client variable.</typeparam>
internal sealed class ClientVar<T> : SerializableVar<T> where T : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClientVar{T}"/> class with a default value.
    /// </summary>
    /// <param name="clientId">The unique identifier of the client that owns this variable.</param>
    /// <param name="varName">The unique key used to identify this variable in the client data store. Spaces are converted to underscores.</param>
    /// <param name="defaultValue">The default value returned when no valid data is available or deserialization fails.</param>
    internal ClientVar(ID clientId, string varName, T defaultValue)
    {
        _clientId = clientId;
        _varName = "client_var:" + varName.ToLower().Replace(' ', '_').Replace("_", "");
        DefaultValue = defaultValue;
    }

    private readonly ID _clientId;
    private readonly string _varName;

    /// <summary>
    /// Gets or sets the current value of the client variable.
    /// </summary>
    internal T Value
    {
        get
        {
            return (T)Deserialize(
                ReloadedLobby.NetworkTransport!.GetLobbyMemberData(
                    ReloadedLobby.LobbyData!.LobbyId,
                    _clientId,
                    _varName),
                typeof(T));
        }
        set
        {
            if (ReloadedClientData.LocalClient?.ClientId == _clientId)
            {
                ReloadedLobby.NetworkTransport!.SetLobbyMemberData(
                    ReloadedLobby.LobbyData!.LobbyId,
                    _varName,
                    Serialize(value!, typeof(T)));

                ReloadedLobby.LobbyData.UpdateLobbyStates();
            }
        }
    }
}
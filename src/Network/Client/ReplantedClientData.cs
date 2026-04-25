using ReplantedOnline.Data.Network;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Structs;

namespace ReplantedOnline.Network.Client;

/// <summary>
/// Represents a networked client in ReplantedOnline, managing ID, client information,
/// and network state for players connected via P2P.
/// </summary>
internal sealed class ReplantedClientData
{
    /// <summary>
    /// Initializes a new instance of the NetClient class.
    /// </summary>
    /// <param name="id">The ID of the client.</param>
    internal ReplantedClientData(ID id)
    {
        ClientId = id;
        Name = ReplantedLobby.NetworkTransport.GetMemberName(id);
        AmLocal = id == ReplantedLobby.NetworkTransport.LocalClientId;
        if (AmLocal)
        {
            LocalClient = this;
        }
        else
        {
            OpponentClient = this;
        }

        Ready = new(id, nameof(Ready), false);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the player is loaded and ready.
    /// </summary>
    internal ClientVar<bool> Ready { get; }

    /// <summary>
    /// Get the local NetClient
    /// </summary>
    internal static ReplantedClientData LocalClient { get; private set; }

    /// <summary>
    /// Get the opponent NetClient
    /// </summary>
    internal static ReplantedClientData OpponentClient { get; private set; }

    /// <summary>
    /// The ID of this client.
    /// </summary>
    internal ID ClientId { get; }

    /// <summary>
    /// The display name of this client.
    /// </summary>
    internal readonly string Name = "Player";

    /// <summary>
    /// Gets whether this client represents the local player.
    /// </summary>
    internal bool AmLocal { get; }

    /// <summary>
    /// Gets whether this client is the host of the current lobby.
    /// </summary>
    internal bool AmHost => ReplantedLobby.AmLobbyHost(ClientId);

    /// <summary>
    /// The team that the player is on.
    /// </summary>
    internal PlayerTeam Team;

    /// <summary>
    /// Gets the plants NetClient
    /// </summary>
    internal static ReplantedClientData GetPlantClient()
    {
        foreach (var client in ReplantedLobby.LobbyData.AllClients.Values)
        {
            if (client.Team == PlayerTeam.Plants)
            {
                return client;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the zombies NetClient
    /// </summary>
    internal static ReplantedClientData GetZombieClient()
    {
        foreach (var client in ReplantedLobby.LobbyData.AllClients.Values)
        {
            if (client.Team == PlayerTeam.Zombies)
            {
                return client;
            }
        }

        return null;
    }
}
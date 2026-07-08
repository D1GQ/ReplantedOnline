using ReplantedOnline.Data.Network;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Structs.Network;

namespace ReplantedOnline.Network.Reloaded.Client;

/// <summary>
/// Represents a networked client in ReplantedOnline, managing ID, client information,
/// and network state for players connected via P2P.
/// </summary>
internal sealed class ReloadedClientData
{
    /// <summary>
    /// Initializes a new instance of the NetClient class.
    /// </summary>
    /// <param name="id">The ID of the client.</param>
    internal ReloadedClientData(ID id)
    {
        ClientId = id;
        Name = ReloadedLobby.NetworkTransport!.GetMemberName(id);
        AmLocal = id == ReloadedLobby.NetworkTransport.LocalClientId;
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
    internal static ReloadedClientData? LocalClient { get; private set; }

    /// <summary>
    /// Get the opponent NetClient
    /// </summary>
    internal static ReloadedClientData? OpponentClient { get; private set; }

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
    internal bool AmHost => ReloadedLobby.AmLobbyHost(ClientId);

    /// <summary>
    /// The team that the player is on.
    /// </summary>
    internal PlayerTeam Team;

    /// <summary>
    /// Gets the 1-based index of this client within the lobby's client list.
    /// </summary>
    /// <returns>
    /// The 1-based index of this client if found; otherwise, <see cref="byte.MaxValue"/>.
    /// </returns>
    internal byte GetClientIndex()
    {
        byte index = 1;
        foreach (var client in ReloadedLobby.LobbyData!.AllClients.Values)
        {
            if (client == this)
            {
                return index;
            }
            index++;
        }

        return byte.MaxValue;
    }

    /// <summary>
    /// Gets the plants NetClient
    /// </summary>
    internal static ReloadedClientData? GetPlantClient()
    {
        if (ReloadedLobby.LobbyData == null)
        {
            return null;
        }

        foreach (var client in ReloadedLobby.LobbyData.AllClients.Values)
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
    internal static ReloadedClientData? GetZombieClient()
    {
        if (ReloadedLobby.LobbyData == null)
        {
            return null;
        }

        foreach (var client in ReloadedLobby.LobbyData.AllClients.Values)
        {
            if (client.Team == PlayerTeam.Zombies)
            {
                return client;
            }
        }

        return null;
    }
}
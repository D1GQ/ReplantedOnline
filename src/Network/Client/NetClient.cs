using MelonLoader;
using ReplantedOnline.Enums;
using ReplantedOnline.Structs;

namespace ReplantedOnline.Network.Client;

/// <summary>
/// Represents a networked client in ReplantedOnline, managing Steam ID, client information,
/// and network state for players connected via Steamworks P2P.
/// </summary>
internal sealed class NetClient
{
    /// <summary>
    /// Initializes a new instance of the SteamNetClient class.
    /// </summary>
    /// <param name="id">The Steam ID of the client.</param>
    internal NetClient(ID id)
    {
        ClientId = id;
        Name = NetLobby.NetworkTransport.GetMemberName(id);
        AmLocal = id == NetLobby.NetworkTransport.LocalClientId;
        if (AmLocal)
        {
            LocalClient = this;
        }
        else
        {
            OpponentClient = this;
        }
        MelonLogger.Msg($"[SteamNetClient] P2P connections initialized with {Name} ({id})");
    }

    /// <summary>
    /// Gets or sets a value indicating whether the player is loaded and ready.
    /// </summary>
    internal bool Ready
    {
        get
        {
            return NetLobby.NetworkTransport.GetLobbyMemberData(NetLobby.LobbyData.LobbyId, ClientId, nameof(Ready)) == bool.TrueString;
        }
        set
        {
            if (AmLocal)
            {
                NetLobby.NetworkTransport.SetLobbyMemberData(NetLobby.LobbyData.LobbyId, nameof(Ready), value.ToString());
            }
        }
    }

    /// <summary>
    /// Get the local SteamNetClient
    /// </summary>
    internal static NetClient LocalClient { get; private set; }

    /// <summary>
    /// Get the opponent SteamNetClient
    /// </summary>
    internal static NetClient OpponentClient { get; private set; }

    /// <summary>
    /// The Steam ID of this client.
    /// </summary>
    internal readonly ID ClientId;

    /// <summary>
    /// The display name of this client from Steam friends.
    /// </summary>
    internal readonly string Name = "Player";

    /// <summary>
    /// Gets whether this client represents the local player.
    /// </summary>
    internal bool AmLocal { get; }

    /// <summary>
    /// Gets whether this client is the host of the current lobby.
    /// </summary>
    internal bool AmHost => NetLobby.AmLobbyHost(ClientId);

    /// <summary>
    /// The team that the player is on.
    /// </summary>
    internal PlayerTeam Team;

    /// <summary>
    /// Gets the plants SteamNetClient
    /// </summary>
    internal static NetClient GetPlantClient()
    {
        foreach (var client in NetLobby.LobbyData.AllClients.Values)
        {
            if (client.Team is PlayerTeam.Plants)
            {
                return client;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the zombies SteamNetClient
    /// </summary>
    internal static NetClient GetZombieClient()
    {
        foreach (var client in NetLobby.LobbyData.AllClients.Values)
        {
            if (client.Team is PlayerTeam.Zombies)
            {
                return client;
            }
        }

        return null;
    }
}
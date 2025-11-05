using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Network;

/// <summary>
/// Represents a networked client in ReplantedOnline, managing Steam ID, client information,
/// and network state for players connected via Steamworks P2P.
/// </summary>
internal class SteamNetClient
{
    /// <summary>
    /// Initializes a new instance of the SteamNetClient class.
    /// </summary>
    /// <param name="id">The Steam ID of the client.</param>
    internal SteamNetClient(SteamId id)
    {
        SteamId = id;
        ClientId = (int)id.AccountId;
        Name = SteamFriends.Internal.GetFriendPersonaName(SteamId);
        AmLocal = id == SteamUser.Internal.GetSteamID();
        if (AmLocal)
        {
            LocalClient = this;
        }
        else
        {
            OpponentClient = this;
        }
        HasEstablishedP2P = AmLocal;
        MelonLogger.Msg($"[SteamNetClient] P2P connections initialized with {Name} ({SteamId})");
    }

    /// <summary>
    /// Get the local SteamNetClient
    /// </summary>
    internal static SteamNetClient LocalClient { get; private set; }

    /// <summary>
    /// Get the opponent SteamNetClient
    /// </summary>
    internal static SteamNetClient OpponentClient { get; private set; }

    /// <summary>
    /// The Steam ID of this client.
    /// </summary>
    internal readonly SteamId SteamId;

    /// <summary>
    /// The client ID derived from the Steam account ID.
    /// </summary>
    internal readonly int ClientId;

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
    internal bool AmHost => NetLobby.AmLobbyHost(SteamId);

    /// <summary>
    /// Gets whether if P2P has been established with this client
    /// </summary>
    internal bool HasEstablishedP2P { get; set; }

    /// <summary>
    /// Gets the playerindex of the client
    /// </summary>
    internal int PlayerIndex
    {
        get
        {
            return AmLocal ? ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX : ReplantedOnlineMod.Constants.OPPONENT_PLAYER_INDEX;
        }
    }

    /// <summary>
    /// Gets if the player is on the zombies side
    /// </summary>
    internal bool AmZombieSide()
    {
        return Instances.GameplayActivity.VersusMode.ZombiePlayerIndex == PlayerIndex;
    }

    /// <summary>
    /// Gets the plants SteamNetClient
    /// </summary>
    internal static SteamNetClient GetPlantClient()
    {
        foreach (var client in NetLobby.LobbyData.AllClients.Values)
        {
            if (!client.AmZombieSide())
            {
                return client;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the zombies SteamNetClient
    /// </summary>
    internal static SteamNetClient GetZombieClient()
    {
        foreach (var client in NetLobby.LobbyData.AllClients.Values)
        {
            if (client.AmZombieSide())
            {
                return client;
            }
        }

        return null;
    }
}
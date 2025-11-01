using Il2CppSteamworks;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Network;

/// <summary>
/// Represents a networked client in ReplantedOnline, managing Steam ID, client information,
/// and network state for players connected via Steamworks P2P.
/// </summary>
internal class SteamNetClient
{
    /// <summary>
    /// List of all currently connected network clients.
    /// </summary>
    internal static List<SteamNetClient> AllClients = [];

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
    internal readonly string Name;

    /// <summary>
    /// Gets whether this client represents the local player.
    /// </summary>
    internal bool IsLocal { get; }

    /// <summary>
    /// Gets whether this client is the host of the current lobby.
    /// </summary>
    internal bool IsHost => NetLobby.IsLobbyHost(SteamId);

    /// <summary>
    /// Initializes a new instance of the SteamNetClient class.
    /// </summary>
    /// <param name="id">The Steam ID of the client.</param>
    internal SteamNetClient(SteamId id)
    {
        SteamId = id;
        ClientId = (int)id.AccountId;
        Name = SteamFriends.Internal.GetFriendPersonaName(SteamId);
        IsLocal = id == SteamUser.Internal.GetSteamID();
    }

    /// <summary>
    /// Retrieves a SteamNetClient instance by Steam ID.
    /// </summary>
    /// <param name="steamId">The Steam ID to search for.</param>
    /// <returns>The SteamNetClient instance if found, otherwise null.</returns>
    internal static SteamNetClient GetBySteamId(SteamId steamId) => AllClients.FirstOrDefault(c => c.SteamId == steamId);

    /// <summary>
    /// Adds a new client to the network client list if it doesn't already exist.
    /// </summary>
    /// <param name="steamId">The Steam ID of the client to add.</param>
    internal static void Add(SteamId steamId)
    {
        if (GetBySteamId(steamId) != default) return;

        var client = new SteamNetClient(steamId);
        AllClients.Add(client);
    }

    /// <summary>
    /// Removes a client from the network client list by Steam ID.
    /// </summary>
    /// <param name="steamId">The Steam ID of the client to remove.</param>
    internal static void Remove(SteamId steamId)
    {
        var client = GetBySteamId(steamId);
        if (client != default)
        {
            AllClients.Remove(client);
        }
    }

    /// <summary>
    /// Clears all clients from the network client list.
    /// </summary>
    internal static void Clear()
    {
        AllClients.Clear();
    }
}
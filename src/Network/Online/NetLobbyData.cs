using Il2CppSteamworks;
using ReplantedOnline.Items.Enums;

namespace ReplantedOnline.Network.Online;

/// <summary>
/// Represents the network data and state for a ReplantedOnline lobby.
/// Manages client information, lobby membership, and game state synchronization.
/// </summary>
internal class NetLobbyData
{
    /// <summary>
    /// Initializes a new instance of the NetLobbyData class with the specified Steam ID.
    /// </summary>
    /// <param name="steamId">The Steam ID of the lobby.</param>
    internal NetLobbyData(SteamId steamId)
    {
        LobbyId = steamId;
    }

    /// <summary>
    /// Gets the Steam ID of this lobby.
    /// </summary>
    internal readonly SteamId LobbyId;

    /// <summary>
    /// Gets or sets the dictionary of all connected clients in the lobby, keyed by their Steam ID.
    /// </summary>
    internal Dictionary<SteamId, SteamNetClient> AllClients = [];

    /// <summary>
    /// Gets a HashSet of all banned players.
    /// </summary>
    internal readonly HashSet<SteamId> Banned = [];

    /// <summary>
    /// Gets or sets the last known game state of the lobby for synchronization purposes.
    /// </summary>
    internal GameState LastGameState = GameState.Lobby;

    /// <summary>
    /// Processes the current list of lobby members, adding new clients and removing disconnected ones.
    /// </summary>
    /// <param name="members">The current list of Steam IDs of members in the lobby.</param>
    internal void ProcessMembers(List<SteamId> members)
    {
        var ids = AllClients.Keys.ToArray();

        // Add new members that aren't already in our client list
        foreach (var member in members)
        {
            if (ids.Contains(member) || Banned.Contains(member)) continue;
            AllClients[member] = new(member);
        }

        // Remove members that are no longer in the lobby or banned
        foreach (var id in ids)
        {
            if (members.Contains(id) && !Banned.Contains(id)) continue;
            AllClients.Remove(id);
        }
    }
}
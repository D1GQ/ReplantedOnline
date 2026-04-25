using ReplantedOnline.Data.Network;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules.Panel;
using ReplantedOnline.Network.Client.Object;
using ReplantedOnline.Network.Routing;
using ReplantedOnline.Structs;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Network.Client;

/// <summary>
/// Represents the network data and state for a ReplantedOnline lobby.
/// Manages client information, lobby membership, and game state synchronization.
/// </summary>
internal sealed class ReplantedLobbyData : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the ReplantedLobbyData class with specified IDs.
    /// </summary>
    /// <param name="lobbyId">The Lobby ID of the lobby.</param>
    /// <param name="hostId">The Client ID of the lobby host.</param>
    internal ReplantedLobbyData(ID lobbyId, ID hostId)
    {
        LobbyId = lobbyId;
        HostId = hostId;
    }

    /// <summary>
    /// Gets the Code of this lobby.
    /// </summary>
    internal string LobbyCode;

    /// <summary>
    /// Gets the Steam ID of this lobby.
    /// </summary>
    internal readonly ID LobbyId;

    /// <summary>
    /// Gets or Sets the Steam ID of the host.
    /// </summary>
    internal readonly ID HostId;

    /// <summary>
    /// Gets or sets the dictionary of all connected clients in the lobby, keyed by their Steam ID.
    /// </summary>
    internal Dictionary<ID, ReplantedClientData> AllClients = [];

    /// <summary>
    /// Gets or sets the dictionary of all network objects spawned.
    /// </summary>
    internal Dictionary<uint, NetworkObject> NetworkObjectsSpawned = [];

    /// <summary>
    /// Network class Id pool for the host client
    /// </summary>
    internal NetworkIdPool NetworkIdPoolHost = new(0, 100000);

    /// <summary>
    /// Network class Id pool for the non host client
    /// </summary>
    internal NetworkIdPool NetworkIdPoolNonHost = new(200000, 300000);

    /// <summary>
    /// Processes the current list of lobby members, adding new clients and removing disconnected ones.
    /// </summary>
    /// <param name="members">The current list of Steam IDs of members in the lobby.</param>
    internal void ProcessMembers(List<ID> members)
    {
        var ids = AllClients.Keys.ToArray();

        // Add new members that aren't already in our client list
        foreach (var member in members)
        {
            if (ids.Contains(member)) continue;
            AllClients[member] = new(member);
        }

        // Remove members that are no longer in the lobby or banned
        foreach (var id in ids)
        {
            if (members.Contains(id)) continue;
            AllClients.Remove(id);
        }

        VersusLobbyManager.UpdateSideVisuals();
    }

    /// <summary>
    /// Determines whether all connected clients are currently marked as ready.
    /// </summary>
    /// <returns>true if every client is ready; otherwise, false.</returns>
    internal bool AllClientsReady() => AllClients.Values.All(c => c.Ready.Value);

    /// <summary>
    /// Sets all clients team to None.
    /// </summary>
    internal void UnsetAllTeams()
    {
        foreach (var client in AllClients.Values)
        {
            client.Team = PlayerTeam.None;
        }
    }

    /// <summary>
    /// Gets the next available network ID for spawning network objects
    /// </summary>
    /// <returns>
    /// The next available network ID, starting from 0 for hosts and 100000 for clients
    /// to ensure ID separation between host and client spawned objects
    /// </returns>
    internal uint GetNextNetworkId() => ReplantedLobby.AmLobbyHost() ? NetworkIdPoolHost.GetUnusedId() : NetworkIdPoolNonHost.GetUnusedId();

    /// <summary>
    /// Handles the spawning of a network object by adding it to the collection of spawned objects
    /// </summary>
    /// <param name="networkObj">The network object to spawn.</param>
    internal void OnNetworkObjectSpawn(NetworkObject networkObj)
    {
        NetworkObjectsSpawned[networkObj.NetworkId] = networkObj;
        networkObj.IsOnNetwork = true;
    }

    /// <summary>
    /// Handles the despawning of a network object by removing it from the collection of spawned objects,
    /// </summary>
    /// <param name="networkObj">The network object to despawn.</param>
    internal void OnNetworkObjectDespawn(NetworkObject networkObj)
    {
        foreach (var netChild in networkObj.ChildNetworkObjects)
        {
            OnNetworkObjectDespawn(netChild);
        }

        ReplantedLobby.LobbyData.NetworkObjectsSpawned.Remove(networkObj.NetworkId);
        networkObj.IsOnNetwork = false;
        networkObj.OnDespawn();
        networkObj.OwnerId = ID.Null;
        networkObj.NetworkId = 0;

        if (!networkObj.AmChild)
        {
            ReplantedLobby.LobbyData.NetworkIdPoolHost.ReleaseId(networkObj.NetworkId);
            ReplantedLobby.LobbyData.NetworkIdPoolNonHost.ReleaseId(networkObj.NetworkId);
        }
    }

    /// <summary>
    /// Locally despawns all network objects and clears the spawned objects dictionary
    /// </summary>
    internal void LocalDespawnAll()
    {
        foreach (var kvp in NetworkObjectsSpawned.ToArray())
        {
            var networkObject = kvp.Value;
            if (networkObject == null)
            {
                NetworkObjectsSpawned.Remove(kvp.Key);
                continue;
            }

            var child = networkObject?.AmChild ?? false;
            if (!child && networkObject.gameObject != null)
            {
                UnityEngine.Object.Destroy(networkObject.gameObject);
            }

            NetworkObjectsSpawned.Remove(kvp.Key);
            if (!child)
            {
                NetworkIdPoolHost.ReleaseId(kvp.Key);
                NetworkIdPoolNonHost.ReleaseId(kvp.Key);
            }
        }
    }

    /// <summary>
    /// Indicates whether the lobby is currently in the process of restarting.
    /// </summary>
    internal LobbyVar<bool> LobbyRestarting { get; } = new(nameof(LobbyRestarting), false);

    /// <summary>
    /// Indicates whether the host is currently selecting their sides/teams.
    /// </summary>
    internal LobbyVar<bool> PickingSides { get; } = new(nameof(PickingSides), false);

    /// <summary>
    /// Indicates whether the game has started.
    /// </summary>
    internal LobbyVar<bool> HasStarted { get; } = new(nameof(HasStarted), false);

    /// <summary>
    /// Gets or sets the team assigned to the host client.
    /// </summary>
    internal LobbyVar<PlayerTeam> HostTeam { get; } = new(nameof(HostTeam), PlayerTeam.None);

    /// <summary>
    /// Gets or sets the currently selected arena for the game.
    /// </summary>
    internal LobbyVar<ArenaTypes> Arena { get; } = new(nameof(Arena), ArenaTypes.Day);

    /// <summary>
    /// Indicates whether the client is ready to process network objects.
    /// </summary>
    internal bool ReadyForNetworkObjects { get; set; } = false;

    /// <summary>
    /// Gets or sets the remaining number of lives for the zombies.
    /// </summary>
    internal int ZombieLife { get; set; } = 3;

    /// <summary>
    /// Initializes lobby data to default.
    /// </summary>
    internal void InitializeData()
    {
        LobbyRestarting.Value = false;
        PickingSides.Value = false;
        HasStarted.Value = false;
        HostTeam.Value = PlayerTeam.None;
        Arena.Value = ArenaTypes.Day;
        ReadyForNetworkObjects = false;
        ZombieLife = 3;

        if (ReplantedLobby.AmLobbyHost())
        {
            MatchmakingManager.UpdateLobbyJoinable();
        }
    }


    /// <summary>
    /// Initiates a lobby reset sequence.
    /// Only the host can trigger a lobby reset.
    /// </summary>
    internal void ResetLobby()
    {
        if (ReplantedLobby.AmLobbyHost())
        {
            LobbyRestarting.Value = true;
            NetworkDispatcher.SendPacket(null, false, PacketHandlerType.ResetLobby, PacketChannel.Main);
            ReplantedLobby.ResetLobby();
        }
    }

    /// <summary>
    /// Updates the lobby states base on lobby data.
    /// </summary>
    internal void UpdateLobbyStates()
    {
        if (HasStarted.Value || LobbyRestarting.Value) return;

        var hostTeam = HostTeam.Value;
        if (hostTeam is PlayerTeam.None)
        {
            VersusLobbyManager.ResetPlayerInput();
            ReplantedLobby.LobbyData.UnsetAllTeams();
            VersusLobbyManager.UpdateSideVisuals();
        }
        else
        {
            var otherTeam = hostTeam.GetOppositeTeam();
            if (ReplantedLobby.AmLobbyHost())
            {
                ReplantedClientData.LocalClient.Team = hostTeam;
                ReplantedClientData.OpponentClient?.Team = otherTeam;
                VersusLobbyManager.SetPlayerInput(hostTeam);
            }
            else
            {
                ReplantedClientData.LocalClient.Team = otherTeam;
                ReplantedClientData.OpponentClient?.Team = hostTeam;
                VersusLobbyManager.SetPlayerInput(otherTeam);
            }

            VersusLobbyManager.UpdateSideVisuals();
        }

        ArenaSelectorPanel.SetPreview(Arena.Value);
    }

    /// <summary>
    /// Dispose of the lobby data.
    /// </summary>
    public void Dispose()
    {
        LocalDespawnAll();
        AllClients?.Clear();
        NetworkObjectsSpawned?.Clear();
        LobbyCode = null;
        NetworkIdPoolHost?.Dispose();
        NetworkIdPoolNonHost?.Dispose();
    }
}
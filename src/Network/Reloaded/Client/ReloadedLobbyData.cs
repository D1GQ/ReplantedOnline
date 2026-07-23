using ReplantedOnline.Data.Network;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Managers.Reloaded;
using ReplantedOnline.Modules.Reloaded.Panel;
using ReplantedOnline.Network.Reloaded.Client.Object;
using ReplantedOnline.Network.Reloaded.Client.Routing;
using ReplantedOnline.Network.Reloaded.Client.Routing.Packet;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Network.Reloaded.Client;

/// <summary>
/// Represents the network data and state for a ReplantedOnline lobby.
/// Manages client information, lobby membership, and game state synchronization.
/// </summary>
internal sealed class ReloadedLobbyData : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the ReplantedLobbyData class with specified IDs.
    /// </summary>
    /// <param name="serverLobby">The Lobby transport instance.</param>
    /// <param name="lobbyId">The Lobby ID of the lobby.</param>
    /// <param name="hostId">The Client ID of the lobby host.</param>
    internal ReloadedLobbyData(ServerLobby serverLobby, ID lobbyId, ID hostId)
    {
        ServerLobby = serverLobby;
        LobbyId = lobbyId;
        HostId = hostId;
    }

    /// <summary>
    /// Gets the lobby transport instance .
    /// </summary>
    internal readonly ServerLobby ServerLobby;

    /// <summary>
    /// Gets the Code of this lobby.
    /// </summary>
    internal string LobbyCode = string.Empty;

    /// <summary>
    /// Gets the Steam ID of this lobby.
    /// </summary>
    internal readonly ID LobbyId = ID.Null;

    /// <summary>
    /// Gets or Sets the Steam ID of the host.
    /// </summary>
    internal readonly ID HostId = ID.Null;

    /// <summary>
    /// Lock object used for thread synchronization when accessing or modifying the AllClients dictionary.
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// Gets or sets the dictionary of all connected clients in the lobby, keyed by their Steam ID.
    /// </summary>
    internal Dictionary<ID, ReloadedClientData> AllClients = [];

    /// <summary>
    /// Gets or sets the dictionary of all network objects spawned.
    /// </summary>
    internal Dictionary<NetworkIdentifier, NetworkObject> NetworkObjectsSpawned = [];

    /// <summary>
    /// Network class Id pool.
    /// </summary>
    internal NetworkIdentifierPool NetworkIdPool = new();

    /// <summary>
    /// Handles the event when a client joins the lobby.
    /// </summary>
    /// <param name="clientId">The unique identifier of the client that joined.</param>
    internal void OnClientJoined(ID clientId)
    {
        ReloadedMatchmaking.UpdateLobbyJoinable(false);
        ReloadedLobby.NetworkTransport!.SetLobbyMemberLimit(LobbyId, ReloadedLobby.MAX_LOBBY_SIZE);

        lock (_lock)
        {
            AllClients[clientId] = new(clientId);
        }

        SortClients();

        VersusLobbyManager.UpdateSideVisuals();

        if (ReloadedLobby.AmLobbyHost())
        {
            ReloadedMatchmaking.UpdateLobbyJoinable();
        }
    }

    /// <summary>
    /// Handles the event when a client leaves the lobby.
    /// </summary>
    /// <param name="clientId">The unique identifier of the client that left.</param>
    internal void OnClientLeft(ID clientId)
    {
        ReloadedMatchmaking.UpdateLobbyJoinable(false);
        ReloadedLobby.NetworkTransport!.SetLobbyMemberLimit(LobbyId, ReloadedLobby.MAX_LOBBY_SIZE);

        lock (_lock)
        {
            AllClients.Remove(clientId);
        }
        ReloadedLobby.NetworkTransport.CloseP2PSessionWithUser(clientId);

        SortClients();

        VersusLobbyManager.UpdateSideVisuals();

        if (ReloadedLobby.AmLobbyHost())
        {
            ReloadedMatchmaking.UpdateLobbyJoinable();
        }
    }

    /// <summary>
    /// Sorts the client list to match the order of members in the lobby.
    /// </summary>
    private void SortClients()
    {
        int memberCount = ReloadedLobby.GetLobbyMemberCount();
        var members = new List<ID>();

        for (int i = 0; i < memberCount; i++)
        {
            var member = ReloadedLobby.GetLobbyMemberByIndex(i);
            members.Add(member);
        }

        var newClients = new Dictionary<ID, ReloadedClientData>();
        foreach (var id in members)
        {
            if (AllClients.TryGetValue(id, out var clientData))
            {
                newClients[id] = clientData;
            }
        }

        lock (_lock)
        {
            AllClients = newClients;
        }
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

        NetworkObjectsSpawned.Remove(networkObj.NetworkId);
        networkObj.IsOnNetwork = false;
        networkObj.OnDespawn();
        networkObj.OwnerId = ID.Null;

        if (!networkObj.AmChild)
        {
            NetworkIdPool.Free(networkObj.NetworkId);
        }

        networkObj.NetworkId = NetworkIdentifier.Null;
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

            var child = networkObject.AmChild;
            if (!child && networkObject.gameObject != null)
            {
                UnityEngine.Object.Destroy(networkObject.gameObject);
            }

            NetworkObjectsSpawned.Remove(kvp.Key);
            if (!child)
            {
                NetworkIdPool.Free(kvp.Key);
            }
        }
    }

    /// <summary>
    /// Indicates whether the lobby is joinable.
    /// </summary>
    internal LobbyVar<bool> LobbyJoinable { get; } = new(nameof(LobbyJoinable), true);

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
    internal LobbyVar<ArenaType> Arena { get; } = new(nameof(Arena), ArenaType.Day);

    /// <summary>
    /// Gets or sets the currently selected Gamemode for the game.
    /// </summary>
    internal VersusGamemodeType Gamemode { get; set; } = default;

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
        NetworkIdPool?.Dispose();
        NetworkIdPool = new();
        LobbyJoinable.Value = true;
        LobbyRestarting.Value = false;
        PickingSides.Value = false;
        HasStarted.Value = false;
        HostTeam.Value = PlayerTeam.None;
        Arena.Value = ArenaType.Day;
        Gamemode = default;
        ReadyForNetworkObjects = false;
        ZombieLife = 3;

        if (ReloadedLobby.AmLobbyHost())
        {
            ReloadedMatchmaking.UpdateLobbyJoinable();
        }
    }


    /// <summary>
    /// Initiates a lobby reset sequence.
    /// Only the host can trigger a lobby reset.
    /// </summary>
    internal void ResetLobby()
    {
        if (ReloadedLobby.AmLobbyHost())
        {
            LobbyRestarting.Value = true;
            NetworkManager.Packet<ResetLobbyPacket>.Singleton.Send();
            ReloadedLobby.ResetLobby();
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
            UnsetAllTeams();
            VersusLobbyManager.UpdateSideVisuals();
        }
        else
        {
            var otherTeam = hostTeam.GetOppositeTeam();
            if (ReloadedLobby.AmLobbyHost())
            {
                ReloadedClientData.LocalClient!.Team = hostTeam;
                ReloadedClientData.OpponentClient?.Team = otherTeam;
                VersusLobbyManager.SetPlayerInput(hostTeam);
            }
            else
            {
                ReloadedClientData.LocalClient!.Team = otherTeam;
                ReloadedClientData.OpponentClient?.Team = hostTeam;
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
        LobbyCode = string.Empty;
        NetworkIdPool?.Dispose();
    }
}
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
    internal bool AllClientsReady() => AllClients.Values.All(c => c.Ready);

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

    internal bool LobbyRestarting
    {
        get
        {
            return ReplantedLobby.NetworkTransport.GetLobbyData(LobbyId, nameof(LobbyRestarting)) == bool.TrueString;
        }
        set
        {
            if (ReplantedLobby.AmLobbyHost())
            {
                ReplantedLobby.NetworkTransport.SetLobbyData(LobbyId, nameof(LobbyRestarting), value.ToString());
                UpdateLobbyStates();
            }
        }
    }

    internal bool PickingSides
    {
        get
        {
            return ReplantedLobby.NetworkTransport.GetLobbyData(LobbyId, nameof(PickingSides)) == bool.TrueString;
        }
        set
        {
            if (ReplantedLobby.AmLobbyHost())
            {
                ReplantedLobby.NetworkTransport.SetLobbyData(LobbyId, nameof(PickingSides), value.ToString());
                UpdateLobbyStates();
            }
        }
    }

    internal bool HasStarted
    {
        get
        {
            return ReplantedLobby.NetworkTransport.GetLobbyData(LobbyId, nameof(HasStarted)) == bool.TrueString;
        }
        set
        {
            if (ReplantedLobby.AmLobbyHost())
            {
                ReplantedLobby.NetworkTransport.SetLobbyData(LobbyId, nameof(HasStarted), value.ToString());
                UpdateLobbyStates();
            }
        }
    }

    internal PlayerTeam HostTeam
    {
        get
        {
            var data = ReplantedLobby.NetworkTransport.GetLobbyData(LobbyId, nameof(HostTeam));
            if (int.TryParse(data, out var @int))
            {
                return (PlayerTeam)@int;
            }

            return PlayerTeam.None;
        }
        set
        {
            if (ReplantedLobby.AmLobbyHost())
            {
                ReplantedLobby.NetworkTransport.SetLobbyData(LobbyId, nameof(HostTeam), ((int)value).ToString());
                UpdateLobbyStates();
            }
        }
    }

    internal ArenaTypes Arena
    {
        get
        {
            var data = ReplantedLobby.NetworkTransport.GetLobbyData(LobbyId, nameof(Arena));
            if (int.TryParse(data, out var @int))
            {
                return (ArenaTypes)@int;
            }

            return ArenaTypes.Day;
        }
        set
        {
            if (ReplantedLobby.AmLobbyHost())
            {
                ReplantedLobby.NetworkTransport.SetLobbyData(LobbyId, nameof(Arena), ((int)value).ToString());
                UpdateLobbyStates();
            }
        }
    }

    /// <summary>
    /// Initializes lobby data to default.
    /// </summary>
    internal void InitializeData()
    {
        LobbyRestarting = false;
        PickingSides = false;
        HasStarted = false;
        HostTeam = PlayerTeam.None;
        Arena = ArenaTypes.Day;

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
            LobbyRestarting = true;
            NetworkDispatcher.SendPacket(null, false, PacketHandlerType.ResetLobby, PacketChannel.Main);
            ReplantedLobby.ResetLobby();
        }
    }

    /// <summary>
    /// Updates the lobby states base on lobby data.
    /// </summary>
    internal void UpdateLobbyStates()
    {
        if (HasStarted || LobbyRestarting) return;

        var hostTeam = HostTeam;
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

        ArenaSelectorPanel.SetPreview(Arena);
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
        GC.SuppressFinalize(this);
    }
}
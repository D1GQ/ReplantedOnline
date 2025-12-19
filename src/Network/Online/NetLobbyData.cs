using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Helper;
using ReplantedOnline.Managers;
using ReplantedOnline.Network.Object;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.RPC;
using ReplantedOnline.Network.RPC.Handlers;
using System.Collections;

namespace ReplantedOnline.Network.Online;

/// <summary>
/// Represents the network data and state for a ReplantedOnline lobby.
/// Manages client information, lobby membership, and game state synchronization.
/// </summary>
internal sealed class NetLobbyData
{
    /// <summary>
    /// Initializes a new instance of the NetLobbyData class with the specified Steam ID.
    /// </summary>
    /// <param name="steamId">The Steam ID of the lobby.</param>
    /// <param name="hostId">The Steam ID of the lobby host.</param>
    internal NetLobbyData(SteamId steamId, SteamId hostId)
    {
        LobbyId = steamId;
        HostId = hostId;
    }

    /// <summary>
    /// Gets the Code of this lobby.
    /// </summary>
    internal string LobbyCode;

    /// <summary>
    /// Gets the Steam ID of this lobby.
    /// </summary>
    internal readonly SteamId LobbyId;

    /// <summary>
    /// Gets or Sets the Steam ID of the host.
    /// </summary>
    internal readonly SteamId HostId;

    /// <summary>
    /// Gets or sets the dictionary of all connected clients in the lobby, keyed by their Steam ID.
    /// </summary>
    internal Dictionary<SteamId, SteamNetClient> AllClients = [];

    /// <summary>
    /// Gets or sets the dictionary of all network classes spawned.
    /// </summary>
    internal Dictionary<uint, NetworkClass> NetworkClassSpawned = [];

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
    internal void ProcessMembers(List<SteamId> members)
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

        VersusManager.UpdateSideVisuals();
    }

    /// <summary>
    /// Determines whether all connected clients are currently marked as ready.
    /// </summary>
    /// <returns>true if every client is ready; otherwise, false.</returns>
    internal bool AllClientsReady() => AllClients.Values.All(c => c.Ready);

    /// <summary>
    /// Marks all clients as not ready by setting their Ready status to false.
    /// </summary>
    internal void UnsetAllClientsReady()
    {
        foreach (var client in AllClients.Values)
        {
            client.Ready = false;
        }
    }

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
    internal uint GetNextNetworkId() => NetLobby.AmLobbyHost() ? NetworkIdPoolHost.GetUnusedId() : NetworkIdPoolNonHost.GetUnusedId();

    internal void OnNetworkClassSpawn(NetworkClass networkClass)
    {
        NetworkClassSpawned[networkClass.NetworkId] = networkClass;
        networkClass.IsOnNetwork = true;
        networkClass.OnSpawn();
    }

    /// <summary>
    /// Locally despawns all network objects and clears the spawned objects dictionary
    /// </summary>
    /// <remarks>
    /// This method destroys all GameObjects associated with network objects
    /// and removes them from the NetworkClassSpawned collection
    /// </remarks>
    internal void LocalDespawnAll()
    {
        foreach (var kvp in NetworkClassSpawned.ToArray())
        {
            var networkObject = kvp.Value;
            if (networkObject == null)
            {
                NetworkClassSpawned.Remove(kvp.Key);
                continue;
            }

            var child = networkObject?.AmChild ?? false;
            if (!child && networkObject.gameObject != null)
            {
                UnityEngine.Object.Destroy(networkObject.gameObject);
            }

            NetworkClassSpawned.Remove(kvp.Key);
            if (!child)
            {
                NetworkIdPoolHost.ReleaseId(kvp.Key);
                NetworkIdPoolNonHost.ReleaseId(kvp.Key);
            }
        }
    }

    internal NetworkedData Networked = new();

    /// <summary>
    /// Handles networked lobby data synchronization between clients.
    /// </summary>
    [RegisterRPCHandler]
    internal sealed class NetworkedData : RPCHandler
    {
        private bool _restartingLobby;
        private bool _hasStarted;
        private PlayerTeam _hostTeam;

        /// <summary>
        /// Gets if the host is picking sides.
        /// </summary>
        internal bool PickingSides => _hostTeam is PlayerTeam.None;

        /// <summary>
        /// Gets whether the versus match has started.
        /// </summary>
        internal bool HasStarted => _hasStarted;

        /// <summary>
        /// Gets which team the host is on.
        /// </summary>
        internal PlayerTeam HostTeam => _hostTeam;

        /// <inheritdoc/>
        internal sealed override RpcType Rpc => RpcType.LobbyData;

        /// <summary>
        /// Sets whether the versus match has started and synchronizes the value across all clients.
        /// </summary>
        /// <param name="value">Whether the match has started.</param>
        internal void SetHasStarted(bool value)
        {
            _hasStarted = value;
            SendData(1, packetWriter =>
            {
                packetWriter.WriteBool(value);
            });
        }

        /// <summary>
        /// Sets which team the host is on and synchronizes the team assignment across all clients.
        /// </summary>
        /// <param name="team">The team for the host.</param>
        internal void SetHostTeam(PlayerTeam team)
        {
            _hostTeam = team;
            SendData(2, packetWriter =>
            {
                packetWriter.WriteByte((byte)team);
            });
        }

        /// <summary>
        /// Initiates a lobby reset sequence.
        /// Only the host can trigger a lobby reset.
        /// </summary>
        internal void ResetLobby()
        {
            if (NetLobby.AmLobbyHost())
            {
                SendData(0);
            }
        }

        /// <summary>
        /// Synchronizes all networked lobby data to newly connected clients.
        /// This ensures late-joining clients receive the current game state.
        /// </summary>
        internal void SendAllData()
        {
            // Send match start status (dataId = 1)
            SendData(1, packetWriter =>
            {
                packetWriter.WriteBool(_hasStarted);
            }, false);

            // Send host team assignment (dataId = 2)
            SendData(2, packetWriter =>
            {
                packetWriter.WriteByte((byte)_hostTeam);
            }, true);
        }

        /// <summary>
        /// Sends a lobby data packet to all connected clients, optionally including additional data and controlling
        /// local receipt.
        /// </summary>
        /// <param name="dataId">The identifier for the type of data to send in the packet.</param>
        /// <param name="callback">An optional callback that writes additional data to the packet before it is sent. If null, only the data
        /// identifier is included.</param>
        /// <param name="receiveLocally">Indicates whether the packet should also be received by the local client. Set to <see langword="true"/> to
        /// deliver the packet locally; otherwise, <see langword="false"/>.</param>
        private void SendData(byte dataId, Action<PacketWriter> callback = null, bool receiveLocally = true)
        {
            var packetWriter = PacketWriter.Get();
            packetWriter.WriteByte(dataId); // Write data type identifier
            callback?.Invoke(packetWriter); // Write additional data if provided
            NetworkDispatcher.SendRpc(RpcType.LobbyData, packetWriter, receiveLocally); // Send to all clients
            packetWriter.Recycle();
        }

        /// <inheritdoc/>
        internal sealed override void Handle(SteamNetClient sender, PacketReader packetReader)
        {
            if (!sender.AmHost) return; // Only accept data from the host

            MelonCoroutines.Start(CoWaitHandle(packetReader));
        }

        /// <summary>
        /// Coroutine that waits for required game systems to be ready before processing RPC data.
        /// </summary>
        /// <param name="packetReader">The packet to process.</param>
        /// <returns>IEnumerator for coroutine execution.</returns>
        private static IEnumerator CoWaitHandle(PacketReader packetReader)
        {
            var packet = PacketReader.Get(packetReader);

            // Wait for the gameplay activity and versus mode to be initialized
            try
            {
                while (NetLobby.LobbyData?.Networked?._restartingLobby != false || !VersusManager.IsUIReady())
                {
                    // If we leave the lobby while waiting, clean up and exit
                    if (!NetLobby.AmInLobby())
                    {
                        packet.Recycle();
                        yield break;
                    }

                    yield return null;
                }

                var data = NetLobby.LobbyData.Networked;
                var dataId = packet.ReadByte();

                switch (dataId)
                {
                    case 0: // Lobby Reset
                        {
                            if (!data._restartingLobby)
                            {
                                data._restartingLobby = true; // Set flag to prevent duplicate resets
                                NetLobby.ResetLobby(); // Execute lobby reset
                            }
                        }
                        break;

                    case 1: // Match Start Status
                        {
                            data._hasStarted = packet.ReadBool(); // Update local match start state
                        }
                        break;

                    case 2: // Enter Team Selection Phase
                        {
                            data._hostTeam = (PlayerTeam)packet.ReadByte();

                            if (data._hostTeam is PlayerTeam.None) // unset teams
                            {
                                VersusManager.ResetPlayerInput();
                                NetLobby.LobbyData.UnsetAllTeams();
                                VersusManager.UpdateSideVisuals();
                            }
                            else
                            {
                                var otherTeam = Utils.GetOppositeTeam(data._hostTeam);
                                if (NetLobby.AmLobbyHost())
                                {
                                    SteamNetClient.LocalClient.Team = data._hostTeam;
                                    SteamNetClient.OpponentClient?.Team = otherTeam;
                                    VersusManager.SetPlayerInput(data._hostTeam);
                                }
                                else
                                {
                                    SteamNetClient.LocalClient.Team = otherTeam;
                                    SteamNetClient.OpponentClient?.Team = data._hostTeam;
                                    VersusManager.SetPlayerInput(otherTeam);
                                }

                                VersusManager.UpdateSideVisuals();
                            }

                            SetReady();
                        }
                        break;
                }
            }
            finally
            {
                packet.Recycle();
            }
        }

        private static void SetReady()
        {
            if (!NetLobby.AmLobbyHost())
            {
                if (!SteamNetClient.LocalClient.Ready)
                {
                    SetClientReadyHandler.Send(); // Ensure we are marked as ready
                }
            }
        }
    }
}
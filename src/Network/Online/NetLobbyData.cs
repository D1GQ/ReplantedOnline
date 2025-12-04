using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Items.Attributes;
using ReplantedOnline.Items.Enums;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.RPC;
using System.Collections;

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
    /// Gets a HashSet of all banned players.
    /// </summary>
    internal readonly HashSet<SteamId> Banned = [];

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

        VersusManager.UpdateSideVisuals();
    }

    /// <summary>
    /// Gets the next available network ID for spawning network objects
    /// </summary>
    /// <returns>
    /// The next available network ID, starting from 0 for hosts and 100000 for clients
    /// to ensure ID separation between host and client spawned objects
    /// </returns>
    internal uint GetNextNetworkId() => NetLobby.AmLobbyHost() ? NetworkIdPoolHost.GetUnusedId() : NetworkIdPoolNonHost.GetUnusedId();

    /// <summary>
    /// Locally despawns all network objects and clears the spawned objects dictionary
    /// </summary>
    /// <remarks>
    /// This method destroys all GameObjects associated with network objects
    /// and removes them from the NetworkClassSpawned collection
    /// </remarks>
    internal void LocalDespawnAll()
    {
        foreach (var kvp in NetworkClassSpawned.ToDictionary(k => k.Key, v => v.Value))
        {
            var child = kvp.Value?.AmChild ?? false;
            if (!child && kvp.Value.gameObject != null)
            {
                UnityEngine.Object.Destroy(kvp.Value.gameObject);
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
    internal class NetworkedData : RPCHandler
    {
        private bool _restartingLobby;
        private bool _hasStarted;
        private bool _pickingSides = true;
        private bool _hostIsOnPlantSide;

        /// <summary>
        /// Gets or sets whether the versus match has started.
        /// Setting this property automatically synchronizes the value across all clients.
        /// </summary>
        internal bool HasStarted
        {
            get
            {
                return _hasStarted;
            }
            set
            {
                // When set by the host, broadcast the new value to all clients
                SendData(1, packetWriter =>
                {
                    packetWriter.WriteBool(value);
                });
            }
        }

        /// <summary>
        /// Gets or sets whether host is currently picking teams.
        /// Setting to true resets all players to the team selection phase.
        /// </summary>
        internal bool PickingSides
        {
            get
            {
                return _pickingSides;
            }
            set
            {
                // Only need to send when entering picking sides mode
                if (value is true)
                {
                    SendData(2);
                }
            }
        }

        /// <summary>
        /// Gets or sets which team the host is on.
        /// Setting this property automatically synchronizes the team assignment across all clients.
        /// </summary>
        internal bool HostIsOnPlantSide
        {
            get
            {
                return _hostIsOnPlantSide;
            }
            set
            {
                SendData(3, packetWriter =>
                {
                    packetWriter.WriteBool(value);
                });
            }
        }

        /// <inheritdoc/>
        internal sealed override RpcType Rpc => RpcType.LobbyData;

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
        internal static void SendAllData()
        {
            // Send match start status (dataId = 1)
            SendData(1, packetWriter =>
            {
                packetWriter.WriteBool(NetLobby.LobbyData.Networked._hasStarted);
            });

            // Send team selection phase status (dataId = 2)
            SendData(2, packetWriter =>
            {
                packetWriter.WriteBool(NetLobby.LobbyData.Networked._pickingSides);
            });

            // Send host team assignment (dataId = 3)
            SendData(3, packetWriter =>
            {
                packetWriter.WriteBool(NetLobby.LobbyData.Networked._hostIsOnPlantSide);
            });
        }

        /// <summary>
        /// Helper method to send RPC data with optional payload.
        /// </summary>
        /// <param name="dataId">The type of data being sent (0-3).</param>
        /// <param name="callback">Optional callback to write additional data to the packet.</param>
        private static void SendData(byte dataId, Action<PacketWriter> callback = null)
        {
            var packetWriter = PacketWriter.Get();
            packetWriter.WriteByte(dataId); // Write data type identifier
            callback?.Invoke(packetWriter); // Write additional data if provided
            NetworkDispatcher.SendRpc(RpcType.LobbyData, packetWriter, true); // Send to all clients
        }

        /// <inheritdoc/>
        internal sealed override void Handle(SteamNetClient sender, PacketReader packetReader)
        {
            if (!sender.AmHost) return; // Only accept data from the host

            var packet = PacketReader.Get(packetReader);
            // Start coroutine to handle the packet, waiting for game systems to be ready
            MelonCoroutines.Start(CoWaitHandle(packet));
        }

        /// <summary>
        /// Coroutine that waits for required game systems to be ready before processing RPC data.
        /// </summary>
        /// <param name="packetReader">The packet to process.</param>
        /// <returns>IEnumerator for coroutine execution.</returns>
        private static IEnumerator CoWaitHandle(PacketReader packetReader)
        {
            // Wait for the gameplay activity and versus mode to be initialized
            while (Instances.GameplayActivity?.VersusMode == null)
            {
                // If we leave the lobby while waiting, clean up and exit
                if (!NetLobby.AmInLobby())
                {
                    packetReader.Recycle();
                    yield break;
                }

                yield return null;
            }

            var dataId = packetReader.ReadByte();
            var data = NetLobby.LobbyData.Networked;

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
                        data._hasStarted = packetReader.ReadBool(); // Update local match start state
                    }
                    break;

                case 2: // Enter Team Selection Phase
                    {
                        data._pickingSides = true; // Enable team picking
                        VersusManager.ResetPlayerInputs(); // Clear previous team assignments
                        VersusManager.UpdateSideVisuals(); // Update UI to show team selection
                    }
                    break;

                case 3: // Lock Teams
                    {
                        data._pickingSides = false; // Disable team picking
                        data._hostIsOnPlantSide = packetReader.ReadBool(); // Get host's team

                        // Assign player inputs based on host's team
                        if (NetLobby.AmLobbyHost())
                        {
                            // If we're the host, assign opposite team to clients
                            VersusManager.UpdatePlayerInputs(!data._hostIsOnPlantSide);
                        }
                        else
                        {
                            // If we're a client, assign same team as host
                            VersusManager.UpdatePlayerInputs(data._hostIsOnPlantSide);
                        }

                        VersusManager.UpdateSideVisuals(); // Update UI with final team assignments
                    }
                    break;
            }

            packetReader.Recycle();
        }
    }
}
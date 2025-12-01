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
    internal NetworkIdPool NetworkIdPoolHost = new(0, 9999);

    /// <summary>
    /// Network class Id pool for the non host client
    /// </summary>
    internal NetworkIdPool NetworkIdPoolNonHost = new(10000, 19999);

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
            if (kvp.Value?.gameObject != null)
            {
                UnityEngine.Object.Destroy(kvp.Value.gameObject);
            }
            NetworkClassSpawned.Remove(kvp.Key);
            NetworkIdPoolHost.ReleaseId(kvp.Key);
            NetworkIdPoolNonHost.ReleaseId(kvp.Key);
        }
    }

    internal NetworkedData Networked = new();

    [RegisterRPCHandler]
    internal class NetworkedData : RPCHandler
    {
        private bool _restartingLobby;
        private bool _hasStarted;
        private bool _pickingSides = true;
        private bool _hostIsOnPlantSide;

        internal bool HasStarted
        {
            get
            {
                return _hasStarted;
            }
            set
            {
                SendData(1, packetWriter =>
                {
                    packetWriter.WriteBool(value);
                });
            }
        }
        internal bool PickingSides
        {
            get
            {
                return _pickingSides;
            }
            set
            {
                if (value is true)
                {
                    SendData(2);
                }
            }
        }

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

        internal void ResetLobby()
        {
            if (NetLobby.AmLobbyHost())
            {
                SendData(0);
            }
        }

        internal static void SendAllData()
        {
            SendData(1, packetWriter =>
            {
                packetWriter.WriteBool(NetLobby.LobbyData.Networked._hasStarted);
            });
            SendData(2, packetWriter =>
            {
                packetWriter.WriteBool(NetLobby.LobbyData.Networked._pickingSides);
            });
            SendData(3, packetWriter =>
            {
                packetWriter.WriteBool(NetLobby.LobbyData.Networked._hostIsOnPlantSide);
            });
        }

        private static void SendData(byte dataId, Action<PacketWriter> callback = null)
        {
            var packetWriter = PacketWriter.Get();
            packetWriter.WriteByte(dataId);
            callback?.Invoke(packetWriter);
            NetworkDispatcher.SendRpc(RpcType.LobbyData, packetWriter, true);
        }

        /// <inheritdoc/>
        internal sealed override void Handle(SteamNetClient sender, PacketReader packetReader)
        {
            if (!sender.AmHost) return;

            var packet = PacketReader.Get(packetReader);
            MelonCoroutines.Start(CoWaitHandle(sender, packet));
        }

        private static IEnumerator CoWaitHandle(SteamNetClient sender, PacketReader packetReader)
        {
            while (Instances.GameplayActivity?.VersusMode == null)
            {
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
                case 0:
                    {
                        if (!data._restartingLobby)
                        {
                            data._restartingLobby = true;
                            NetLobby.ResetLobby();
                        }
                    }
                    break;
                case 1:
                    {
                        data._hasStarted = packetReader.ReadBool();
                    }
                    break;
                case 2:
                    {
                        data._pickingSides = true;
                        VersusManager.ResetPlayerInputs();
                        VersusManager.UpdateSideVisuals();
                    }
                    break;
                case 3:
                    {
                        data._pickingSides = false;
                        data._hostIsOnPlantSide = packetReader.ReadBool();
                        if (NetLobby.AmLobbyHost())
                        {
                            VersusManager.UpdatePlayerInputs(!data._hostIsOnPlantSide);
                        }
                        else
                        {
                            VersusManager.UpdatePlayerInputs(data._hostIsOnPlantSide);
                        }
                        VersusManager.UpdateSideVisuals();
                    }
                    break;
            }

            packetReader.Recycle();
        }
    }
}
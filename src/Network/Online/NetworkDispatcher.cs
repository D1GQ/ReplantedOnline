using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Helper;
using ReplantedOnline.Items.Enums;
using ReplantedOnline.Items.Interfaces;
using ReplantedOnline.Network.Object;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.RPC;
using UnityEngine;

namespace ReplantedOnline.Network.Online;

/// <summary>
/// Handles network packet dispatching and reception for ReplantedOnline.
/// Manages sending packets to connected clients and processing incoming packets via RPC system.
/// </summary>
internal static class NetworkDispatcher
{
    /// <summary>
    /// Spawns a network class instance and broadcasts it to all connected clients.
    /// Initializes the network object with ownership and network ID before sending spawn packet.
    /// </summary>
    /// <param name="networkClass">The network class instance to spawn.</param>
    /// <param name="owner">The Steam ID of the owner who controls this network object.</param>
    internal static void Spawn(NetworkClass networkClass, SteamId owner)
    {
        networkClass.OwnerId = owner;
        networkClass.NetworkId = NetLobby.LobbyData.GetNextNetworkId();
        NetLobby.LobbyData.NetworkClassSpawned[networkClass.NetworkId] = networkClass;
        var packet = PacketWriter.Get();
        NetworkSpawnPacket.SerializePacket(networkClass, packet);
        Send(packet, false, PacketTag.NetworkClassSpawn);
        networkClass.HasSpawned = true;

        packet.Recycle();
        MelonLogger.Msg($"[NetworkDispatcher] Spawned NetworkClass with ID: {networkClass.NetworkId}, Owner: {owner}");
    }

    /// <summary>
    /// Sends a packet to a specific client in the lobby by their Steam ID.
    /// Automatically skips sending to the local client to prevent self-processing.
    /// </summary>
    /// <param name="steamId">The Steam ID of the target client to receive the packet.</param>
    /// <param name="packetWriter">The packet writer containing the data to send.</param>
    /// <param name="tag">The packet tag identifying the packet type.</param>
    internal static void SendTo(SteamId steamId, PacketWriter packetWriter, PacketTag tag = PacketTag.None)
    {
        if (steamId.GetNetClient().AmLocal) return;

        var packet = PacketWriter.Get();
        packet.AddTag(tag);
        packet.WritePacket(packetWriter);

        if (NetLobby.IsPlayerInOurLobby(steamId))
        {
            SteamNetworking.SendP2PPacket(steamId, packet.GetBytes(), packet.Length);
        }

        MelonLogger.Msg($"[NetworkDispatcher] Sent {tag} packet to {steamId.GetNetClient().Name} -> Size: {packet.Length} bytes");
        packet.Recycle();
    }

    /// <summary>
    /// Sends a packet to all connected clients in the lobby.
    /// </summary>
    /// <param name="packetWriter">The packet writer containing the data to send.</param>
    /// <param name="receiveLocally">Whether the local client should also process this packet.</param>
    /// <param name="tag">The packet tag identifying the packet type.</param>
    internal static void Send(PacketWriter packetWriter, bool receiveLocally, PacketTag tag = PacketTag.None)
    {
        var packet = PacketWriter.Get();
        packet.AddTag(tag);
        packet.WritePacket(packetWriter);

        int sentCount = 0;
        foreach (var client in NetLobby.LobbyData.AllClients.Values)
        {
            if (client.AmLocal) continue;

            if (NetLobby.IsPlayerInOurLobby(client.SteamId))
            {
                bool sent = SteamNetworking.SendP2PPacket(client.SteamId, packet.GetBytes(), packet.Length);
                if (sent) sentCount++;
            }
        }

        if (receiveLocally)
        {
            var rePacket = PacketReader.Get(packet.GetBytes());
            Streamline(SteamNetClient.LocalClient, rePacket);
            rePacket.Recycle();
        }

        MelonLogger.Msg($"[NetworkDispatcher] Sent {tag} packet to {sentCount} clients -> Size: {packet.Length} bytes");
        packet.Recycle();
    }

    /// <summary>
    /// Sends an RPC (Remote Procedure Call) to all connected clients.
    /// </summary>
    /// <param name="rpc">The type of RPC to send.</param>
    /// <param name="packetWriter">The packet writer containing RPC-specific data.</param>
    /// <param name="receiveLocally">Whether the local client should also process this RPC.</param>
    internal static void SendRpc(RpcType rpc, PacketWriter packetWriter = null, bool receiveLocally = false)
    {
        MelonLogger.Msg($"[NetworkDispatcher] Sending RPC: {rpc}");
        var packet = PacketWriter.Get();
        packet.WriteByte((byte)rpc);
        if (packetWriter != null)
        {
            packet.WritePacket(packetWriter);
        }

        Send(packet, receiveLocally, PacketTag.Rpc);

        packetWriter?.Recycle();
        packet.Recycle();
    }

    /// <summary>
    /// Sends an RPC (Remote Procedure Call) to a specific network class instance across all clients.
    /// Used for invoking targeted RPC methods on specific network objects.
    /// </summary>
    /// <param name="networkClass">The target network class instance to receive the RPC.</param>
    /// <param name="rpcId">The ID of the RPC method to invoke.</param>
    /// <param name="packetWriter">The packet writer containing RPC-specific data.</param>
    /// <param name="receiveLocally">Whether the local client should also process this RPC.</param>
    internal static void SendRpc(this INetworkClass networkClass, byte rpcId, PacketWriter packetWriter = null, bool receiveLocally = false)
    {
        var packet = PacketWriter.Get();
        packet.WriteByte(rpcId);
        packet.WriteUInt(networkClass.NetworkId);
        if (packetWriter != null)
        {
            packet.WritePacket(packetWriter);
        }

        Send(packet, receiveLocally, PacketTag.NetworkClassRpc);

        packetWriter?.Recycle();
        packet.Recycle();

        MelonLogger.Msg($"[NetworkDispatcher] Sent NetworkClass RPC: {rpcId} for NetworkId: {networkClass.NetworkId}");
    }

    /// <summary>
    /// Processes all available incoming P2P packets.
    /// Called regularly to handle network communication.
    /// </summary>
    internal static void Update()
    {
        if (!NetLobby.AmInLobby()) return;

        foreach (var networkClass in NetLobby.LobbyData.NetworkClassSpawned.Values)
        {
            if (!networkClass.AmOwner || !networkClass.IsDirty) continue;
            var packet = PacketWriter.Get();
            packet.WriteUInt(networkClass.NetworkId);
            packet.WriteUInt(networkClass.DirtyBits);
            networkClass.Serialize(packet, false);
            Send(packet, false, PacketTag.NetworkClassSync);
        }

        while (SteamNetworking.IsP2PPacketAvailable(out uint messageSize))
        {
            var buffer = P2PPacketBuffer.Get();

            buffer.EnsureCapacity(messageSize);
            buffer.Size = messageSize;
            buffer.Steamid = 0;

            if (SteamNetworking.ReadP2PPacket(buffer.Data, ref buffer.Size, ref buffer.Steamid))
            {
                if (buffer.Steamid.Banned())
                {
                    MelonLogger.Msg($"[NetworkDispatcher] Discarded packet from banned player: {buffer.Steamid}");
                    buffer.Recycle();
                    continue;
                }

                var sender = buffer.Steamid.GetNetClient();
                MelonLogger.Msg($"[NetworkDispatcher] Received packet from {sender.Name} ({buffer.Steamid}) -> Size: {buffer.Size} bytes");

                if (buffer.Size > 0)
                {
                    var receivedData = buffer.ToByteArray();
                    var packetReader = PacketReader.Get(receivedData);
                    Streamline(sender, packetReader);
                }
                else
                {
                    MelonLogger.Error("[NetworkDispatcher] Received packet with zero size");
                }
            }
            else
            {
                MelonLogger.Error("[NetworkDispatcher] Failed to read P2P packet from network buffer");
            }

            buffer.Recycle();
        }
    }

    /// <summary>
    /// Processes an incoming packet based on its tag and routes it to the appropriate handler.
    /// </summary>
    /// <param name="sender">The client that sent the packet.</param>
    /// <param name="packetReader">The packet reader containing the packet data.</param>
    internal static void Streamline(SteamNetClient sender, PacketReader packetReader)
    {
        var tag = packetReader.GetTag();
        MelonLogger.Msg($"[NetworkDispatcher] Processing {tag} packet from {sender?.Name ?? "Unknown"}");

        switch (tag)
        {
            case PacketTag.None:
                MelonLogger.Warning("[NetworkDispatcher] Received packet with no tag");
                break;
            case PacketTag.P2P:
                sender.HasEstablishedP2P = true;
                MelonLogger.Msg("[NetworkDispatcher] P2P handshake packet processed");
                break;
            case PacketTag.P2PClose:
                if (sender.AmHost && !NetLobby.AmLobbyHost())
                {
                    BanReasons reason = (BanReasons)packetReader.ReadByte();
                    NetLobby.LeaveLobby();
                    MelonLogger.Msg("[NetworkDispatcher] P2P closed by host");
                }
                break;
            case PacketTag.Rpc:
                StreamlineRpc(sender, packetReader);
                break;
            case PacketTag.NetworkClassSpawn:
                StreamlineNetworkClassSpawn(sender, packetReader);
                break;
            case PacketTag.NetworkClassDespawn:
                StreamlineNetworkClassDespawn(sender, packetReader);
                break;
            case PacketTag.NetworkClassSync:
                StreamlineNetworkClassSync(sender, packetReader);
                break;
            case PacketTag.NetworkClassRpc:
                StreamlineNetworkClassRpc(sender, packetReader);
                break;
            default:
                MelonLogger.Warning($"[NetworkDispatcher] Unknown packet tag: {tag}");
                break;
        }

        packetReader.Recycle();
    }

    /// <summary>
    /// Processes an incoming RPC packet and routes it to the appropriate RPC handler.
    /// </summary>
    /// <param name="sender">The client that sent the RPC.</param>
    /// <param name="packetReader">The packet reader containing the RPC data.</param>
    private static void StreamlineRpc(SteamNetClient sender, PacketReader packetReader)
    {
        RpcType rpc = (RpcType)packetReader.ReadByte();
        MelonLogger.Msg($"[NetworkDispatcher] Processing RPC from {sender.Name}: {rpc}");
        RPCHandler.HandleRpc(rpc, sender, packetReader);
    }

    /// <summary>
    /// Processes an incoming network class spawn packet and instantiates the appropriate object.
    /// Handles both custom-created network objects and prefab-based network objects.
    /// </summary>
    /// <param name="sender">The client that sent the spawn packet.</param>
    /// <param name="packetReader">The packet reader containing spawn data.</param>
    private static void StreamlineNetworkClassSpawn(SteamNetClient sender, PacketReader packetReader)
    {
        var spawnPacket = NetworkSpawnPacket.DeserializePacket(packetReader);
        if (spawnPacket.PrefabId == NetworkClass.NO_PREFAB_ID)
        {
            var networkClass = new GameObject("???").AddComponent<NetworkClass>();
            networkClass.transform.SetParent(NetworkClass.NetworkClassesObj.transform);
            networkClass.OwnerId = spawnPacket.OwnerId;
            networkClass.NetworkId = spawnPacket.NetworkId;
            networkClass.Deserialize(packetReader, true);
            networkClass.HasSpawned = true;
            networkClass.name = $"{networkClass.GetType().Name}({networkClass.NetworkId})";
            NetLobby.LobbyData.NetworkClassSpawned[networkClass.NetworkId] = networkClass;
            MelonLogger.Msg($"[NetworkDispatcher] Spawned custom NetworkClass from {sender.Name}: {spawnPacket.NetworkId}");
        }
        else
        {
            if (NetworkClass.NetworkPrefabs.TryGetValue(spawnPacket.PrefabId, out var prefab))
            {
                var networkClass = UnityEngine.Object.Instantiate(prefab);
                networkClass.transform.SetParent(NetworkClass.NetworkClassesObj.transform);
                networkClass.OwnerId = spawnPacket.OwnerId;
                networkClass.NetworkId = spawnPacket.NetworkId;
                networkClass.Deserialize(packetReader, true);
                networkClass.gameObject.SetActive(true);
                networkClass.HasSpawned = true;
                networkClass.name = $"{networkClass.GetType().Name}({networkClass.NetworkId})";
                NetLobby.LobbyData.NetworkClassSpawned[networkClass.NetworkId] = networkClass;
                MelonLogger.Msg($"[NetworkDispatcher] Spawned prefab NetworkClass from {sender.Name}: {spawnPacket.NetworkId}, Prefab: {spawnPacket.PrefabId}");
            }
            else
            {
                MelonLogger.Error($"[NetworkDispatcher] Failed to spawn NetworkClass: Prefab ID {spawnPacket.PrefabId} not found");
            }
        }
    }

    /// <summary>
    /// Processes an incoming network class despawn packet and destroys the corresponding object.
    /// Cleans up network objects that are no longer needed in the scene.
    /// </summary>
    /// <param name="sender">The client that sent the despawn packet.</param>
    /// <param name="packetReader">The packet reader containing despawn data.</param>
    private static void StreamlineNetworkClassDespawn(SteamNetClient sender, PacketReader packetReader)
    {
        uint networkId = packetReader.ReadUInt();
        if (NetLobby.LobbyData.NetworkClassSpawned.TryGetValue(networkId, out var networkClass))
        {
            if (networkClass.OwnerId == sender.SteamId)
            {
                NetLobby.LobbyData.NetworkClassSpawned.Remove(networkId);
                UnityEngine.Object.Destroy(networkClass.gameObject);
                MelonLogger.Msg($"[NetworkDispatcher] Despawned NetworkClass from {sender.Name}: {networkId}");
            }
        }
        else
        {
            MelonLogger.Warning($"[NetworkDispatcher] Failed to despawn NetworkClass: ID {networkId} not found");
        }
    }

    /// <summary>
    /// Processes an incoming network class synchronization packet and updates the corresponding object.
    /// Handles state synchronization for network objects from remote clients.
    /// </summary>
    /// <param name="sender">The client that sent the sync packet.</param>
    /// <param name="packetReader">The packet reader containing synchronization data.</param>
    private static void StreamlineNetworkClassSync(SteamNetClient sender, PacketReader packetReader)
    {
        uint networkId = packetReader.ReadUInt();
        uint syncedBits = packetReader.ReadUInt();
        if (NetLobby.LobbyData.NetworkClassSpawned.TryGetValue(networkId, out var networkClass))
        {
            if (networkClass.OwnerId != sender.SteamId) return;

            networkClass.SyncedBits.SyncedDirtyBits = syncedBits;
            networkClass.Deserialize(packetReader, false);
            MelonLogger.Msg($"[NetworkDispatcher] Synced NetworkClass from {sender.Name}: {networkId}");
        }
        else
        {
            MelonLogger.Warning($"[NetworkDispatcher] Failed to sync NetworkClass: ID {networkId} not found");
        }
    }

    /// <summary>
    /// Processes an incoming network class RPC packet and routes it to the appropriate handler.
    /// Handles remote procedure calls targeted at specific network objects.
    /// </summary>
    /// <param name="sender">The client that sent the RPC.</param>
    /// <param name="packetReader">The packet reader containing RPC data.</param>
    private static void StreamlineNetworkClassRpc(SteamNetClient sender, PacketReader packetReader)
    {
        byte rpcId = packetReader.ReadByte();
        uint networkId = packetReader.ReadUInt();
        if (NetLobby.LobbyData.NetworkClassSpawned.TryGetValue(networkId, out var networkClass))
        {
            MelonLogger.Msg($"[NetworkDispatcher] Processing NetworkClass RPC from {sender.Name}: {rpcId} for NetworkId: {networkId}");
            networkClass.HandleRpc(sender, rpcId, packetReader);
        }
        else
        {
            MelonLogger.Warning($"[NetworkDispatcher] Failed to process NetworkClass RPC: ID {networkId} not found");
        }
    }
}
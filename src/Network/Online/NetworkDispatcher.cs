using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Enums;
using ReplantedOnline.Helper;
using ReplantedOnline.Interfaces;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.RPC;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Online;

/// <summary>
/// Handles network packet dispatching and reception for ReplantedOnline.
/// Manages sending packets to connected clients and processing incoming packets via RPC system.
/// </summary>
internal static class NetworkDispatcher
{
    /// <summary>
    /// Spawns all Active network classes to a new client
    /// </summary>
    /// <param name="steamId">The Steam ID of the target client to receive the packet.</param>
    internal static void SendNetworkClasssTo(SteamId steamId)
    {
        if (NetLobby.LobbyData.NetworkClassSpawned.Count > 0)
        {
            foreach (var networkClass in NetLobby.LobbyData.NetworkClassSpawned.Values)
            {
                if (networkClass.IsOnNetwork)
                {
                    var packet = PacketWriter.Get();
                    NetworkSpawnPacket.SerializePacket(networkClass, packet);
                    SendPacketTo(steamId, packet, PacketTag.NetworkClassSpawn, PacketChannel.Buffered);
                    packet.Recycle();
                }
            }
        }
    }

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
        NetLobby.LobbyData.OnNetworkClassSpawn(networkClass);
        var packet = PacketWriter.Get();
        NetworkSpawnPacket.SerializePacket(networkClass, packet);
        SendPacket(packet, false, PacketTag.NetworkClassSpawn, PacketChannel.Main);

        packet.Recycle();
        MelonLogger.Msg($"[NetworkDispatcher] Spawned NetworkClass with ID: {networkClass.NetworkId}, Owner: {owner}");
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

        SendPacket(packet, receiveLocally, PacketTag.Rpc, PacketChannel.Rpc);

        packetWriter?.Recycle();
        packet.Recycle();
    }

    internal static void SendRpc(this INetworkClass networkClass, byte rpcId, bool receiveLocally = false)
        => SendRpc(networkClass, rpcId, null, receiveLocally);

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

        SendPacket(packet, receiveLocally, PacketTag.NetworkClassRpc, PacketChannel.Rpc);

        packetWriter?.Recycle();
        packet.Recycle();

        MelonLogger.Msg($"[NetworkDispatcher] Sent NetworkClass RPC: {rpcId} for NetworkId: {networkClass.NetworkId}");
    }

    /// <summary>
    /// Sends a packet to a specific client in the lobby by their Steam ID.
    /// Automatically skips sending to the local client to prevent self-processing.
    /// </summary>
    /// <param name="steamId">The Steam ID of the target client to receive the packet.</param>
    /// <param name="packetWriter">The packet writer containing the data to send.</param>
    /// <param name="tag">The packet tag identifying the packet type.</param>
    /// <param name="packetChannel">The channel to send the packet on.</param>
    internal static void SendPacketTo(SteamId steamId, PacketWriter packetWriter, PacketTag tag, PacketChannel packetChannel)
    {
        if (steamId.GetNetClient().AmLocal) return;

        var packet = PacketWriter.Get();
        packet.AddTag(tag);
        packet.WritePacket(packetWriter);

        if (NetLobby.IsPlayerInOurLobby(steamId))
        {
            var sendType = packetChannel is PacketChannel.Buffered ? P2PSend.ReliableWithBuffering : P2PSend.Reliable;
            SteamNetworking.SendP2PPacket(steamId, packet.GetBytes(), packet.Length, (int)packetChannel, sendType);
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
    /// <param name="packetChannel">The channel to send the packet on.</param>
    internal static void SendPacket(PacketWriter packetWriter, bool receiveLocally, PacketTag tag, PacketChannel packetChannel)
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
                var sendType = packetChannel is PacketChannel.Buffered ? P2PSend.ReliableWithBuffering : P2PSend.Reliable;
                bool sent = SteamNetworking.SendP2PPacket(client.SteamId, packet.GetBytes(), packet.Length, (int)packetChannel, sendType);
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
    /// Processes all available incoming P2P packets.
    /// Called regularly to handle network communication.
    /// </summary>
    internal static void Update()
    {
        if (!NetLobby.AmInLobby()) return;

        foreach (var networkClass in NetLobby.LobbyData.NetworkClassSpawned.Values)
        {
            if (!networkClass.AmOwner || !networkClass.IsOnNetwork || !networkClass.IsDirty) continue;
            var packet = PacketWriter.Get();
            NetworkSyncPacket.SerializePacket(networkClass, false, packet);
            SendPacket(packet, false, PacketTag.NetworkClassSync, PacketChannel.Buffered);
        }

        while (SteamNetworking.IsP2PPacketAvailable(out uint messageSize, (int)PacketChannel.Main))
        {
            ReadPacket(messageSize, (int)PacketChannel.Main);
        }

        while (SteamNetworking.IsP2PPacketAvailable(out uint messageSize, (int)PacketChannel.Buffered))
        {
            ReadPacket(messageSize, (int)PacketChannel.Buffered);
        }

        while (SteamNetworking.IsP2PPacketAvailable(out uint messageSize, (int)PacketChannel.Rpc))
        {
            ReadPacket(messageSize, (int)PacketChannel.Rpc);
        }
    }

    /// <summary>
    /// Reads and processes a single P2P packet from the specified network channel.
    /// Handles packet reception, buffer management, and routing to the appropriate packet handler.
    /// Validates packet integrity and sender authentication before processing.
    /// </summary>
    private static void ReadPacket(uint messageSize, int channel)
    {
        var buffer = P2PPacketBuffer.Get();

        buffer.EnsureCapacity(messageSize);
        buffer.Size = messageSize;
        buffer.Steamid = 0;

        if (SteamNetworking.ReadP2PPacket(buffer.Data, ref buffer.Size, ref buffer.Steamid, channel))
        {
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
                    NetLobby.LeaveLobby(() =>
                    {
                        ReplantedOnlinePopup.Show("Disconnected", "You have been disconnected by the Host!");
                    });
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
            MelonLogger.Error("Serialized network class had a unset prefab id!");
        }
        else
        {
            if (NetworkClass.NetworkPrefabs.TryGetValue(spawnPacket.PrefabId, out var prefab))
            {
                var networkClass = prefab.Clone<NetworkClass>();
                networkClass.OwnerId = spawnPacket.OwnerId;
                networkClass.NetworkId = spawnPacket.NetworkId;
                NetLobby.LobbyData.NetworkClassSpawned[networkClass.NetworkId] = networkClass;
                networkClass.transform.SetParent(NetworkClass.NetworkClassesObj.transform);
                networkClass.gameObject.SetActive(true);
                NetworkSpawnPacket.DeserializeNetworkClass(networkClass, packetReader);
                networkClass.IsOnNetwork = true;
                networkClass.name = $"{networkClass.GetType().Name}({networkClass.NetworkId})";
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
                if (!networkClass.AmChild)
                {
                    networkClass.Despawn(false);
                    UnityEngine.Object.Destroy(networkClass.gameObject);
                    MelonLogger.Msg($"[NetworkDispatcher] Despawned NetworkClass from {sender.Name}: {networkId}");
                }
                else
                {
                    MelonLogger.Error($"[NetworkDispatcher] {sender.Name} Client requested to despawn child network class {networkId}, only the parent can be despawned!");
                }
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
        MelonCoroutines.Start(CoWaitForNetworkClassSync(sender, packetReader));
    }

    private static IEnumerator CoWaitForNetworkClassSync(SteamNetClient sender, PacketReader packetReader)
    {
        packetReader = PacketReader.Get(packetReader);
        var networkSyncPacket = NetworkSyncPacket.DeserializePacket(packetReader);

        while (NetLobby.LobbyData != null)
        {
            if (NetLobby.LobbyData.NetworkClassSpawned.TryGetValue(networkSyncPacket.NetworkId, out var networkClass))
            {
                if (networkClass.OwnerId != sender.SteamId)
                {
                    MelonLogger.Warning($"[NetworkDispatcher] Sync rejected: {sender.Name} is not owner of NetworkClass {networkSyncPacket.NetworkId}");
                    yield break;
                }

                networkClass.SyncedBits.SyncedDirtyBits = networkSyncPacket.DirtyBits;
                networkClass.Deserialize(packetReader, networkSyncPacket.Init);
                MelonLogger.Msg($"[NetworkDispatcher] Synced NetworkClass from {sender.Name}: {networkSyncPacket.NetworkId}");
                yield break;
            }

            yield return null;
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
        MelonCoroutines.Start(CoWaitForNetworkClass(sender, packetReader));
    }

    private static IEnumerator CoWaitForNetworkClass(SteamNetClient sender, PacketReader packetReader)
    {
        packetReader = PacketReader.Get(packetReader);
        byte rpcId = packetReader.ReadByte();
        uint networkId = packetReader.ReadUInt();
        float timeOut = 0f;

        try
        {
            while (NetLobby.LobbyData != null && timeOut < 10f)
            {
                if (NetLobby.LobbyData.NetworkClassSpawned.TryGetValue(networkId, out var networkClass))
                {
                    MelonLogger.Msg($"[NetworkDispatcher] Processing NetworkClass RPC from {sender.Name}: {rpcId} for NetworkId: {networkId}");
                    networkClass.HandleRpc(sender, rpcId, packetReader);
                    yield break;
                }

                timeOut += Time.deltaTime;

                yield return null;
            }
        }
        finally
        {
            packetReader.Recycle();
        }
    }
}
using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Enums;
using ReplantedOnline.Helper;
using ReplantedOnline.Interfaces;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object;
using ReplantedOnline.Network.Online.PacketHandler;
using ReplantedOnline.Network.Packet;
using System.Collections;

namespace ReplantedOnline.Network.Online;

/// <summary>
/// Handles network packet dispatching and reception for ReplantedOnline.
/// Manages sending packets to connected clients and processing incoming packets via RPC system.
/// </summary>
internal static class NetworkDispatcher
{
    /// <summary>
    /// Spawns all Active network objects to a new client
    /// </summary>
    /// <param name="steamId">The Steam ID of the target client to receive the packet.</param>
    internal static void SendNetworkObjectsTo(SteamId steamId)
    {
        MelonLogger.Msg($"[NetworkDispatcher] Sending network objects to {steamId}");

        if (NetLobby.LobbyData.NetworkObjectsSpawned.Count > 0)
        {
            foreach (var networkObj in NetLobby.LobbyData.NetworkObjectsSpawned.Values)
            {
                if (networkObj.IsOnNetwork && !networkObj.AmChild)
                {
                    var packet = PacketWriter.Get();
                    NetworkSpawnPacket.SerializePacket(networkObj, packet);
                    SendPacketTo(steamId, packet, PacketTag.NetworkClassSpawn, PacketChannel.Buffered);
                    packet.Recycle();
                }
            }
        }
    }

    /// <summary>
    /// Spawns a network object instance and broadcasts it to all connected clients.
    /// Initializes the network object with ownership and network ID before sending spawn packet.
    /// </summary>
    /// <param name="networkObj">The network object instance to spawn.</param>
    /// <param name="owner">The Steam ID of the owner who controls this network object.</param>
    internal static void SpawnNetworkObject(NetworkObject networkObj, SteamId owner)
    {
        networkObj.OwnerId = owner;
        networkObj.NetworkId = NetLobby.LobbyData.GetNextNetworkId();
        NetLobby.LobbyData.OnNetworkObjectSpawn(networkObj);
        var packet = PacketWriter.Get();
        NetworkSpawnPacket.SerializePacket(networkObj, packet);
        SendPacket(packet, false, PacketTag.NetworkClassSpawn, PacketChannel.Main);
        packet.Recycle();

        MelonLogger.Msg($"[NetworkDispatcher] Spawned Network Object with ID: {networkObj.NetworkId}, Owner: {owner}");
    }

    /// <summary>
    /// Despawns a network object instance.
    /// </summary>
    /// <param name="networkObj">The network object instance to despawn.</param>
    internal static void DespawnNetworkObject(NetworkObject networkObj)
    {
        MelonLogger.Msg($"[NetworkDispatcher] Despawning Network Object with ID: {networkObj.NetworkId}");

        var packet = PacketWriter.Get();
        NetworkDespawnPacket.SerializePacket(networkObj, packet);
        SendPacket(packet, false, PacketTag.NetworkClassDespawn, PacketChannel.Main);
        packet.Recycle();

        NetLobby.LobbyData.OnNetworkObjectDespawn(networkObj);
    }

    /// <summary>
    /// Sends an RPC (Remote Procedure Call) to all connected clients.
    /// </summary>
    /// <param name="rpc">The type of RPC to send.</param>
    /// <param name="packetWriter">The packet writer containing RPC-specific data.</param>
    /// <param name="receiveLocally">Whether the local client should also process this RPC.</param>
    internal static void SendRpc(ClientRpcType rpc, PacketWriter packetWriter = null, bool receiveLocally = false)
    {
        var packet = PacketWriter.Get();
        packet.WriteByte((byte)rpc);
        if (packetWriter != null)
        {
            packet.WritePacket(packetWriter);
        }
        SendPacket(packet, receiveLocally, PacketTag.Rpc, PacketChannel.Rpc);
        packet.Recycle();
        MelonLogger.Msg($"[NetworkDispatcher] Sent RPC: {Enum.GetName(rpc)}");
    }

    /// <summary>
    /// Sends an RPC (Remote Procedure Call) to a specific network object instance across all clients.
    /// Used for invoking targeted RPC methods on specific network objects.
    /// </summary>
    /// <param name="networkObj">The target network object instance to receive the RPC.</param>
    /// <param name="rpcId">The ID of the RPC method to invoke.</param>
    /// <param name="packetWriter">The packet writer containing RPC-specific data.</param>
    /// <param name="receiveLocally">Whether the local client should also process this RPC.</param>
    internal static void SendRpc(this INetworkObject networkObj, byte rpcId, PacketWriter packetWriter = null, bool receiveLocally = false)
    {
        var packet = PacketWriter.Get();
        packet.WriteByte(rpcId);
        packet.WriteUInt(networkObj.NetworkId);
        if (packetWriter != null)
        {
            packet.WritePacket(packetWriter);
        }
        SendPacket(packet, receiveLocally, PacketTag.NetworkClassRpc, PacketChannel.Rpc);
        packet.Recycle();
        MelonLogger.Msg($"[NetworkDispatcher] Sent NetworkClass RPC: {rpcId} for NetworkId: {networkObj.NetworkId}");
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
        }

        MelonLogger.Msg($"[NetworkDispatcher] Sent {tag} packet to {sentCount} clients -> Size: {packet.Length} bytes");
        packet.Recycle();
    }

    private static object listeningToken;

    /// <summary>
    /// Starts the network packet listening coroutine.
    /// Stops any existing listening coroutine before starting a new one.
    /// </summary>
    internal static void StartListening()
    {
        if (listeningToken != null)
        {
            MelonCoroutines.Stop(listeningToken);
        }

        listeningToken = MelonCoroutines.Start(CoListening());
    }

    private static int processed;

    /// <summary>
    /// Coroutine that handles network packet processing with per-frame limits.
    /// </summary>
    /// <returns>Enumerator for coroutine execution</returns>
    internal static IEnumerator CoListening()
    {
        MelonLogger.Msg("Starting NetworkDispatcher");

        while (NetLobby.AmInLobby())
        {
            foreach (var networkObj in NetLobby.LobbyData.NetworkObjectsSpawned.Values)
            {
                if (!networkObj.AmOwner || !networkObj.IsOnNetwork || !networkObj.IsDirty) continue;
                var packet = PacketWriter.Get();
                NetworkSyncPacket.SerializePacket(networkObj, false, packet);
                SendPacket(packet, false, PacketTag.NetworkClassSync, PacketChannel.Buffered);
                packet.Recycle();
            }

            processed = 5;
            while (SteamNetworking.IsP2PPacketAvailable(out uint messageSize, (int)PacketChannel.Rpc))
            {
                if (processed <= 0) break;
                ReadPacket(messageSize, (int)PacketChannel.Rpc);
                processed--;
            }

            processed = 5;
            while (SteamNetworking.IsP2PPacketAvailable(out uint messageSize, (int)PacketChannel.Main))
            {
                if (processed <= 0) break;
                ReadPacket(messageSize, (int)PacketChannel.Main);
                processed--;
            }

            processed = 5;
            while (SteamNetworking.IsP2PPacketAvailable(out uint messageSize, (int)PacketChannel.Buffered))
            {
                if (processed <= 0) break;
                ReadPacket(messageSize, (int)PacketChannel.Buffered);
                processed--;
            }

            yield return null;
        }

        MelonLogger.Msg("Stoping NetworkDispatcher");

        listeningToken = null;
    }

    /// <summary>
    /// Reads and processes a single P2P packet from the specified network channel.
    /// Handles packet reception, buffer management, and routing to the appropriate packet handler.
    /// </summary>
    private static void ReadPacket(uint messageSize, int channel)
    {
        var buffer = P2PPacketBuffer.Get(messageSize);

        try
        {
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
        }
        finally
        {
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

        try
        {
            switch (tag)
            {
                case PacketTag.None:
                    MelonLogger.Warning("[NetworkDispatcher] Received packet with no tag");
                    break;
                case PacketTag.RemoveClient:
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
                default:
                    if (!BasePacketHandler.HandlePacket(tag, sender, packetReader))
                    {
                        MelonLogger.Warning($"[NetworkDispatcher] Unknown packet tag: {tag}");
                    }
                    break;
            }
        }
        finally
        {
            packetReader.Recycle();
        }
    }
}
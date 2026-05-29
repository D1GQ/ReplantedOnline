using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Reloaded.Panel;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Client.Object;
using ReplantedOnline.Network.Routing.Packet;
using ReplantedOnline.Network.Routing.Packet.Messages;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.MelonLoader;
using ReplantedOnline.Utilities.Modded;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Routing;

/// <summary>
/// Handles network packet dispatching and reception for ReplantedOnline.
/// Manages sending packets to connected clients and processing incoming packets via RPC system.
/// </summary>
internal static class NetworkDispatcher
{
    /// <summary>
    /// Spawns all Active network objects to a new client
    /// </summary>
    /// <param name="targetId">The ID of the target client to receive the packet.</param>
    internal static void SendNetworkObjectsTo(ID targetId)
    {
        if (ReloadedLobby.LobbyData.NetworkObjectsSpawned.Count > 0)
        {
            foreach (var networkObj in ReloadedLobby.LobbyData.NetworkObjectsSpawned.Values)
            {
                if (networkObj.IsOnNetwork && !networkObj.AmChild)
                {
                    var packet = PacketWriter.Get();
                    Message<NetworkObjectSpawnMessage>.Instance.Serialize(networkObj, packet);

                    ReplantedOnlineMod.Logger.Msg(typeof(NetworkDispatcher), $"Sent Network Objects to {targetId}");
                    SendPacketTo(targetId, packet, PacketHandlerType.NetworkObjectSpawn, PacketChannel.Buffered);
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
    /// <param name="owner">The ID of the owner who controls this network object.</param>
    internal static void SpawnNetworkObject(NetworkObject networkObj, ID owner)
    {
        networkObj.OwnerId = owner;
        networkObj.NetworkId = ReloadedLobby.LobbyData.NetworkIdPool.Allocate();
        var packet = PacketWriter.Get();
        Message<NetworkObjectSpawnMessage>.Instance.Serialize(networkObj, packet);

        ReplantedOnlineMod.Logger.Msg(typeof(NetworkDispatcher), $"Sent Spawn Network Object with ID: {networkObj.NetworkId}, Owner: {owner}");
        if (ReloadedLobby.AmLobbyHost())
        {
            SendPacket(packet, false, PacketHandlerType.NetworkObjectSpawn, PacketChannel.Main);
        }
        else
        {
            SendPacketTo(ReloadedLobby.LobbyData.HostId, packet, PacketHandlerType.NetworkObjectSpawnCmd, PacketChannel.Main);
        }
        packet.Recycle();
    }

    /// <summary>
    /// Rejects a network object spawn request and notifies the requesting client.
    /// </summary>
    /// <param name="networkId">The network identifier of the object being rejected.</param>
    /// <param name="owner">The ID of the owner who requested the spawn.</param>
    internal static void RejectNetworkObject(NetworkIdentifier networkId, ID owner)
    {
        if (owner.GetNetClient().AmLocal) return;

        var packet = PacketWriter.Get();
        Message<NetworkObjectRejectMessage>.Instance.Serialize(networkId, packet);

        ReplantedOnlineMod.Logger.Msg(typeof(NetworkDispatcher), $"Sent Reject Network Object with ID: {networkId}, Owner: {owner}");
        SendPacketTo(owner, packet, PacketHandlerType.NetworkObjectReject, PacketChannel.Main);
        packet.Recycle();
    }

    /// <summary>
    /// Despawns a network object instance.
    /// </summary>
    /// <param name="networkObj">The network object instance to despawn.</param>
    /// <param name="waitToBeReady">Indicate whether the network object should wait until locally want to despawn on the other side .</param>
    internal static void DespawnNetworkObject(NetworkObject networkObj, bool waitToBeReady)
    {
        var packet = PacketWriter.Get();
        Message<NetworkObjectDespawnMessage>.Instance.Serialize(networkObj, waitToBeReady, packet);

        ReplantedOnlineMod.Logger.Msg(typeof(NetworkDispatcher), $"Sent Despawn Network Object with ID: {networkObj.NetworkId}");
        SendPacket(packet, false, PacketHandlerType.NetworkObjectDespawn, PacketChannel.Main);
        packet.Recycle();

        ReloadedLobby.LobbyData.OnNetworkObjectDespawn(networkObj);
    }

    /// <summary>
    /// Sends an RPC (Remote Procedure Call) to all connected clients.
    /// </summary>
    /// <param name="rpc">The type of RPC to send.</param>
    /// <param name="payload">The packet writer containing RPC-specific data.</param>
    /// <param name="receiveLocally">Whether the local client should also process this RPC.</param>
    internal static void SendRpc(RpcType rpc, PacketWriter payload = null, bool receiveLocally = false)
    {
        var packet = PacketWriter.Get();
        Message<RpcMessage>.Instance.Serialize(rpc, packet);
        if (payload != null)
        {
            packet.WritePacketToBuffer(payload);
        }

        ReplantedOnlineMod.Logger.Msg(typeof(NetworkDispatcher), $"Sent RPC: {Enum.GetName(rpc)}");
        SendPacket(packet, receiveLocally, PacketHandlerType.Rpc, PacketChannel.Rpc);
        packet.Recycle();
    }

    /// <summary>
    /// Sends an RPC (Remote Procedure Call) to a specific INetworkIdentifier instance across all clients.
    /// </summary>
    /// <param name="networkIdentifier">The target INetworkIdentifier instance to receive the RPC.</param>
    /// <param name="rpcId">The ID of the RPC method to invoke.</param>
    /// <param name="payload">The packet writer containing RPC-specific data.</param>
    /// <param name="receiveLocally">Whether the local client should also process this RPC.</param>
    internal static void SendRpcReceiver(INetworkIdentifier networkIdentifier, byte rpcId, PacketWriter payload = null, bool receiveLocally = false)
    {
        var packet = PacketWriter.Get();
        Message<ObjectRpcMessage>.Instance.Serialize(networkIdentifier, rpcId, packet);
        if (payload != null)
        {
            packet.WritePacketToBuffer(payload);
        }

        ReplantedOnlineMod.Logger.Msg(typeof(NetworkDispatcher), $"Sent Object RPC: {rpcId} for NetworkId: {networkIdentifier.NetworkId}");
        SendPacket(packet, receiveLocally, PacketHandlerType.ObjectRpc, PacketChannel.Rpc);
        packet.Recycle();
    }

    /// <summary>
    /// Sends a packet to a specific client in the lobby by their ID.
    /// </summary>
    /// <param name="targetId">The ID of the target client to receive the packet.</param>
    /// <param name="payload">The packet writer containing the data to send.</param>
    /// <param name="tag">The packet tag identifying the packet type.</param>
    /// <param name="packetChannel">The channel to send the packet on.</param>
    internal static void SendPacketTo(ID targetId, PacketWriter payload, PacketHandlerType tag, PacketChannel packetChannel)
    {
        if (targetId.GetNetClient().AmLocal) return;

        var packet = PacketWriter.Get();
        Message<PacketHeaderMessage>.Instance.Serialize(tag, payload, packet);

        if (ReloadedLobby.IsPlayerInOurLobby(targetId))
        {
            var sendType = packetChannel is PacketChannel.Buffered ? P2PSend.ReliableWithBuffering : P2PSend.Reliable;
            ReloadedLobby.NetworkTransport.SendP2PPacket(targetId, packet.GetByteBuffer(), packet.Length, packetChannel, sendType);
        }

        ReplantedOnlineMod.Logger.Msg(typeof(NetworkDispatcher), $"Sent {tag} packet to {targetId.GetNetClient().Name} -> Size: {packet.Length} bytes");
        packet.Recycle();
    }

    /// <summary>
    /// Sends a packet to all connected clients in the lobby.
    /// </summary>
    /// <param name="payload">The packet writer containing the data to send.</param>
    /// <param name="receiveLocally">Whether the local client should also process this packet.</param>
    /// <param name="tag">The packet tag identifying the packet type.</param>
    /// <param name="packetChannel">The channel to send the packet on.</param>
    /// <param name="ignoredClientIds">Optional array of client IDs that should not receive this packet.</param>
    internal static void SendPacket(PacketWriter payload, bool receiveLocally, PacketHandlerType tag, PacketChannel packetChannel, params ID[] ignoredClientIds)
    {
        var packet = PacketWriter.Get();
        Message<PacketHeaderMessage>.Instance.Serialize(tag, payload, packet);

        int sentCount = 0;
        foreach (var client in ReloadedLobby.LobbyData.AllClients.Values)
        {
            if (client.AmLocal) continue;
            if (ignoredClientIds.Contains(client.ClientId)) continue;

            if (ReloadedLobby.IsPlayerInOurLobby(client.ClientId))
            {
                var sendType = packetChannel is PacketChannel.Buffered ? P2PSend.ReliableWithBuffering : P2PSend.Reliable;
                bool sent = ReloadedLobby.NetworkTransport.SendP2PPacket(client.ClientId, packet.GetByteBuffer(), packet.Length, packetChannel, sendType);
                if (sent) sentCount++;
            }
        }

        ReplantedOnlineMod.Logger.Msg(typeof(NetworkDispatcher), $"Sent {tag} packet to {sentCount} clients -> Size: {packet.Length} bytes");

        if (receiveLocally)
        {
            var rePacket = PacketReader.Get(packet.GetByteBuffer());
            Streamline(ReloadedClientData.LocalClient, rePacket, true);
        }

        packet.Recycle();
    }

    private static object ListeningToken;

    /// <summary>
    /// Starts the network packet listening coroutine.
    /// Stops any existing listening coroutine before starting a new one.
    /// </summary>
    internal static void StartListening()
    {
        if (ListeningToken != null)
        {
            MelonCoroutines.Stop(ListeningToken);
        }

        ListeningToken = MelonCoroutines.Start(CoListening());
    }

    private static int Processed;

    /// <summary>
    /// Coroutine that handles network packet processing with per-frame limits.
    /// </summary>
    /// <returns>Enumerator for coroutine execution</returns>
    internal static IEnumerator CoListening()
    {
        ReplantedOnlineMod.Logger.Msg(typeof(NetworkDispatcher), "Starting...");

        while (ReloadedLobby.AmInLobby())
        {
            try
            {
                ReloadedLobby.NetworkTransport.Tick(Time.deltaTime);

                foreach (var networkObj in ReloadedLobby.LobbyData?.NetworkObjectsSpawned.Values)
                {
                    if (!networkObj.AmOwner || !networkObj.IsOnNetwork || !networkObj.IsDirty) continue;
                    var packet = PacketWriter.Get();
                    Message<NetworkObjectSyncMessage>.Instance.Serialize(networkObj, false, packet);
                    SendPacket(packet, false, PacketHandlerType.NetworkObjectSync, PacketChannel.Buffered);
                    packet.Recycle();
                }

                Processed = 5;
                while (ReloadedLobby.NetworkTransport.IsP2PPacketAvailable(out uint messageSize, PacketChannel.Rpc))
                {
                    if (Processed <= 0) break;
                    ReadPacket(messageSize, PacketChannel.Rpc);
                    Processed--;
                }

                Processed = 5;
                while (ReloadedLobby.NetworkTransport.IsP2PPacketAvailable(out uint messageSize, PacketChannel.Main))
                {
                    if (Processed <= 0) break;
                    ReadPacket(messageSize, PacketChannel.Main);
                    Processed--;
                }

                Processed = 5;
                while (ReloadedLobby.NetworkTransport.IsP2PPacketAvailable(out uint messageSize, PacketChannel.Buffered))
                {
                    if (Processed <= 0) break;
                    ReadPacket(messageSize, PacketChannel.Buffered);
                    Processed--;
                }
            }
            catch (Exception ex)
            {
                ReplantedOnlineMod.Logger.Error(typeof(NetworkDispatcher), $"Exception in CoListening: {ex}");
                ReloadedLobby.LeaveLobby(() =>
                {
                    CustomPopupPanel.Show("Error", "An error occurred while processing network packets.");
                });
                ListeningToken = null;
                yield break;
            }

            yield return null;
        }

        ReplantedOnlineMod.Logger.Msg(typeof(NetworkDispatcher), "Stoping...");

        ListeningToken = null;
    }

    /// <summary>
    /// Reads and processes a single P2P packet from the specified network channel.
    /// Handles packet reception, buffer management, and routing to the appropriate packet handler.
    /// </summary>
    private static void ReadPacket(uint messageSize, PacketChannel channel)
    {
        var buffer = P2PPacketBuffer.Get(messageSize);

        try
        {
            if (ReloadedLobby.NetworkTransport.ReadP2PPacket(buffer, channel))
            {
                var sender = buffer.ClientId.GetNetClient();
                ReplantedOnlineMod.Logger.Msg(typeof(NetworkDispatcher), $"Received packet from {sender.Name} ({buffer.ClientId}) -> Size: {buffer.Size} bytes");

                if (buffer.Size > 0)
                {
                    var receivedData = buffer.ToByteArray();
                    if (receivedData == null)
                    {
                        return;
                    }
                    var packetReader = PacketReader.Get(receivedData);
                    Streamline(sender, packetReader, false);
                }
                else
                {
                    ReplantedOnlineMod.Logger.Error(typeof(NetworkDispatcher), "Received packet with zero size");
                }
            }
            else
            {
                ReplantedOnlineMod.Logger.Error(typeof(NetworkDispatcher), "Failed to read P2P packet from network buffer");
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
    /// <param name="local">Whether if this packet is from the local client.</param>
    internal static void Streamline(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        var message = Message<PacketHeaderMessage>.Instance.Deserialize(packetReader);

        if (message.SignatureHash != ModInfo.ModSignature.SignatureHash)
        {
            if (!local)
            {
                ReplantedOnlineMod.Logger.Warning(typeof(NetworkDispatcher), $"Can not processing {message.HandlerType} packet from {sender?.Name ?? "Unknown"}, SignatureHash does not match ({ModInfo.ModSignature.SignatureHash} != {message.SignatureHash})");
            }

            packetReader.Recycle();
            return;
        }

        if (!local)
        {
            ReplantedOnlineMod.Logger.Msg(typeof(NetworkDispatcher), $"Processing {message.HandlerType} packet from {sender?.Name ?? "Unknown"}");
        }

        try
        {
            switch (message.HandlerType)
            {
                case PacketHandlerType.None:
                    if (!local)
                    {
                        ReplantedOnlineMod.Logger.Warning(typeof(NetworkDispatcher), "Received packet with no tag");
                    }
                    else
                    {
                        ReplantedOnlineMod.Logger.Warning(typeof(NetworkDispatcher), "Received local packet with no tag");
                    }
                    break;
                case PacketHandlerType.RemoveClient:
                    if (local) break;
                    if (sender.AmHost && !ReloadedLobby.AmLobbyHost())
                    {
                        var reason = packetReader.ReadEnum<BanReasons>();
                        ReloadedLobby.LeaveLobby(() =>
                        {
                            CustomPopupPanel.Show("Disconnected", "You have been disconnected by the Host!");
                        });
                        ReplantedOnlineMod.Logger.Msg(typeof(NetworkDispatcher), "P2P closed by host");
                    }
                    break;
                case PacketHandlerType.ResetLobby:
                    if (sender.AmHost)
                    {
                        ReloadedLobby.ResetLobby();
                    }
                    break;
                default:
                    if (!IPacketHandler.HandlePacket(message.HandlerType, sender, packetReader, local))
                    {
                        ReplantedOnlineMod.Logger.Warning(typeof(NetworkDispatcher), $"Unknown packet tag: {message.HandlerType}");
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
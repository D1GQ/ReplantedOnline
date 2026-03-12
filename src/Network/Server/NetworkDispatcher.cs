using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Enums;
using ReplantedOnline.Interfaces;
using ReplantedOnline.Modules.Panels;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Object;
using ReplantedOnline.Network.Server.Packet;
using ReplantedOnline.Network.Server.PacketHandler;
using ReplantedOnline.Structs;
using ReplantedOnline.Utilities;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Server;

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
        ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Sending network objects to {targetId}");

        if (NetLobby.LobbyData.NetworkObjectsSpawned.Count > 0)
        {
            foreach (var networkObj in NetLobby.LobbyData.NetworkObjectsSpawned.Values)
            {
                if (networkObj.IsOnNetwork && !networkObj.AmChild)
                {
                    var packet = PacketWriter.Get();
                    NetworkSpawnPacket.SerializePacket(networkObj, packet);
                    SendPacketTo(targetId, packet, PacketTag.NetworkClassSpawn, PacketChannel.Buffered);
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
        networkObj.NetworkId = NetLobby.LobbyData.GetNextNetworkId();
        NetLobby.LobbyData.OnNetworkObjectSpawn(networkObj);
        var packet = PacketWriter.Get();
        NetworkSpawnPacket.SerializePacket(networkObj, packet);
        SendPacket(packet, false, PacketTag.NetworkClassSpawn, PacketChannel.Main);
        packet.Recycle();

        ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Spawned Network Object with ID: {networkObj.NetworkId}, Owner: {owner}");
    }

    /// <summary>
    /// Despawns a network object instance.
    /// </summary>
    /// <param name="networkObj">The network object instance to despawn.</param>
    internal static void DespawnNetworkObject(NetworkObject networkObj)
    {
        ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Despawning Network Object with ID: {networkObj.NetworkId}");

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
        ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Sent RPC: {Enum.GetName(rpc)}");
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
        ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Sent NetworkClass RPC: {rpcId} for NetworkId: {networkObj.NetworkId}");
    }

    /// <summary>
    /// Sends a packet to a specific client in the lobby by their ID.
    /// Automatically skips sending to the local client to prevent self-processing.
    /// </summary>
    /// <param name="targetId">The ID of the target client to receive the packet.</param>
    /// <param name="packetWriter">The packet writer containing the data to send.</param>
    /// <param name="tag">The packet tag identifying the packet type.</param>
    /// <param name="packetChannel">The channel to send the packet on.</param>
    internal static void SendPacketTo(ID targetId, PacketWriter packetWriter, PacketTag tag, PacketChannel packetChannel)
    {
        if (targetId.GetNetClient().AmLocal) return;

        var packet = PacketWriter.Get();
        packet.AddTag(tag);
        if (packetWriter != null)
        {
            packet.WritePacket(packetWriter);
        }

        if (NetLobby.IsPlayerInOurLobby(targetId))
        {
            var sendType = packetChannel is PacketChannel.Buffered ? P2PSend.ReliableWithBuffering : P2PSend.Reliable;
            NetLobby.NetworkTransport.SendP2PPacket(targetId, packet.GetBytes(), packet.Length, packetChannel, sendType);
        }

        ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Sent {tag} packet to {targetId.GetNetClient().Name} -> Size: {packet.Length} bytes");
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
        if (packetWriter != null)
        {
            packet.WritePacket(packetWriter);
        }

        int sentCount = 0;
        foreach (var client in NetLobby.LobbyData.AllClients.Values)
        {
            if (client.AmLocal) continue;

            if (NetLobby.IsPlayerInOurLobby(client.ClientId))
            {
                var sendType = packetChannel is PacketChannel.Buffered ? P2PSend.ReliableWithBuffering : P2PSend.Reliable;
                bool sent = NetLobby.NetworkTransport.SendP2PPacket(client.ClientId, packet.GetBytes(), packet.Length, packetChannel, sendType);
                if (sent) sentCount++;
            }
        }

        if (receiveLocally)
        {
            var rePacket = PacketReader.Get(packet.GetBytes());
            Streamline(NetClient.LocalClient, rePacket);
        }

        ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Sent {tag} packet to {sentCount} clients -> Size: {packet.Length} bytes");
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
        ReplantedOnlineMod.Logger.Msg("[NetworkDispatcher] Starting NetworkDispatcher");

        while (NetLobby.AmInLobby())
        {
            try
            {
                NetLobby.NetworkTransport.Tick(Time.deltaTime);

                foreach (var networkObj in NetLobby.LobbyData?.NetworkObjectsSpawned.Values)
                {
                    if (!networkObj.AmOwner || !networkObj.IsOnNetwork || !networkObj.IsDirty) continue;
                    var packet = PacketWriter.Get();
                    NetworkSyncPacket.SerializePacket(networkObj, false, packet);
                    SendPacket(packet, false, PacketTag.NetworkClassSync, PacketChannel.Buffered);
                    packet.Recycle();
                }

                processed = 5;
                while (NetLobby.NetworkTransport.IsP2PPacketAvailable(out uint messageSize, PacketChannel.Rpc))
                {
                    if (processed <= 0) break;
                    ReadPacket(messageSize, PacketChannel.Rpc);
                    processed--;
                }

                processed = 5;
                while (NetLobby.NetworkTransport.IsP2PPacketAvailable(out uint messageSize, PacketChannel.Main))
                {
                    if (processed <= 0) break;
                    ReadPacket(messageSize, PacketChannel.Main);
                    processed--;
                }

                processed = 5;
                while (NetLobby.NetworkTransport.IsP2PPacketAvailable(out uint messageSize, PacketChannel.Buffered))
                {
                    if (processed <= 0) break;
                    ReadPacket(messageSize, PacketChannel.Buffered);
                    processed--;
                }
            }
            catch (Exception ex)
            {
                ReplantedOnlineMod.Logger.Error($"[NetworkDispatcher] Exception in CoListening: {ex}");
                NetLobby.LeaveLobby(() =>
                {
                    CustomPopupPanel.Show("Error", "An error occurred while processing network packets.");
                });
                listeningToken = null;
                yield break;
            }

            yield return null;
        }

        ReplantedOnlineMod.Logger.Msg("[NetworkDispatcher] Stoping NetworkDispatcher");

        listeningToken = null;
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
            if (NetLobby.NetworkTransport.ReadP2PPacket(buffer, channel))
            {
                var sender = buffer.ClientId.GetNetClient();
                ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Received packet from {sender.Name} ({buffer.ClientId}) -> Size: {buffer.Size} bytes");

                if (buffer.Size > 0)
                {
                    var receivedData = buffer.ToByteArray();
                    var packetReader = PacketReader.Get(receivedData);
                    Streamline(sender, packetReader);
                }
                else
                {
                    ReplantedOnlineMod.Logger.Error("[NetworkDispatcher] Received packet with zero size");
                }
            }
            else
            {
                ReplantedOnlineMod.Logger.Error("[NetworkDispatcher] Failed to read P2P packet from network buffer");
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
    internal static void Streamline(NetClient sender, PacketReader packetReader)
    {
        var tag = packetReader.GetTag();
        ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Processing {tag} packet from {sender?.Name ?? "Unknown"}");

        try
        {
            switch (tag)
            {
                case PacketTag.None:
                    ReplantedOnlineMod.Logger.Warning("[NetworkDispatcher] Received packet with no tag");
                    break;
                case PacketTag.RemoveClient:
                    if (sender.AmHost && !NetLobby.AmLobbyHost())
                    {
                        BanReasons reason = (BanReasons)packetReader.ReadByte();
                        NetLobby.LeaveLobby(() =>
                        {
                            CustomPopupPanel.Show("Disconnected", "You have been disconnected by the Host!");
                        });
                        ReplantedOnlineMod.Logger.Msg("[NetworkDispatcher] P2P closed by host");
                    }
                    break;
                case PacketTag.ResetLobby:
                    if (sender.AmHost)
                    {
                        NetLobby.ResetLobby();
                    }
                    break;
                default:
                    if (!BasePacketHandler.HandlePacket(tag, sender, packetReader))
                    {
                        ReplantedOnlineMod.Logger.Warning($"[NetworkDispatcher] Unknown packet tag: {tag}");
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
using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Reloaded.Panel;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Network.Reloaded.Serialization.Messages;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.MelonLoader;
using ReplantedOnline.Utilities.Modded;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Reloaded.Client.Routing;

/// <summary>
/// Manages network communication, packet routing, and synchronization of network objects in the Reloaded client.
/// </summary>
internal static partial class NetworkManager
{
    /// <summary>
    /// Sends a packet to all connected clients in the lobby.
    /// </summary>
    /// <param name="payload">The packet containing the data to send.</param>
    /// <param name="tag">The packet tag identifying the packet type.</param>
    /// <param name="packetChannel">The channel to send the packet on.</param>
    /// <param name="log">If sending should be logged.</param>
    /// <param name="receiveLocally">Whether the local client should also process this packet.</param>
    /// <param name="ignoredClientIds">Optional array of client IDs that should not receive this packet.</param>
    internal static void SendPacket(IPacket? payload, PacketType tag, PacketChannel packetChannel, bool log, bool receiveLocally, params ID[] ignoredClientIds)
    {
        foreach (var client in ReloadedLobby.LobbyData!.AllClients.Values)
        {
            if (ignoredClientIds.Contains(client.ClientId)) continue;
            if (client.AmLocal && !receiveLocally) continue;

            if (ReloadedLobby.IsPlayerInOurLobby(client.ClientId))
            {
                SendPacketTo(client.ClientId, payload, tag, packetChannel, log);
            }
        }
    }

    /// <summary>
    /// Sends a packet to a specific client in the lobby by their ID.
    /// </summary>
    /// <param name="targetId">The ID of the target client to receive the packet.</param>
    /// <param name="payload">The packet writer containing the data to send.</param>
    /// <param name="tag">The packet tag identifying the packet type.</param>
    /// <param name="packetChannel">The channel to send the packet on.</param>
    /// <param name="log">If sending should be logged.</param>
    internal static void SendPacketTo(ID targetId, IPacket? payload, PacketType tag, PacketChannel packetChannel, bool log)
    {
        if (!targetId.TryGetClientData(out var client))
        {
            return;
        }

        PacketWriter packetWriter = PacketWriter.Get();
        packetWriter.StartSubpacket((byte)tag);
        if (payload != null)
        {
            packetWriter.WritePacketToBuffer(payload);
        }
        packetWriter.EndSubpacket();

        if (client.AmLocal == true)
        {
            ProcessPacketData(ReloadedClientData.LocalClient, packetWriter.GetByteBuffer());
            return;
        }

        if (ReloadedLobby.IsPlayerInOurLobby(targetId))
        {
            var sendType = packetChannel is PacketChannel.Buffered ? P2PSend.ReliableWithBuffering : P2PSend.Reliable;
            ReloadedLobby.NetworkTransport!.SendP2PPacket(targetId, packetWriter.GetByteBuffer(), packetChannel, sendType);
        }

        if (log)
        {
            ReplantedOnlineMod.Logger.Msg(typeof(NetworkManager), $"Sent {tag} packet to {client.Name} -> Size: {packetWriter.Length} bytes");
        }

        packetWriter.Recycle();
    }

    private static object? ListeningToken;

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

        Heartbeat.Start();
        ListeningToken = MelonCoroutines.Start(CoListening());
    }

    private static int Processed;

    /// <summary>
    /// Coroutine that handles network packet processing with per-frame limits.
    /// </summary>
    /// <returns>Enumerator for coroutine execution</returns>
    internal static IEnumerator CoListening()
    {
        ReplantedOnlineMod.Logger.Msg(typeof(NetworkManager), "Starting...");

        while (ReloadedLobby.AmInLobby())
        {
            try
            {
                ReloadedLobby.NetworkTransport!.Tick(Time.deltaTime);
                NetworkHeartbeat.Tick();

                if (ReloadedLobby.LobbyData != null)
                {
                    foreach (var networkObj in ReloadedLobby.LobbyData.NetworkObjectsSpawned.Values)
                    {
                        if (!networkObj.AmOwner || !networkObj.IsOnNetwork || !networkObj.IsDirty) continue;
                        var packet = PacketWriter.Get();
                        Message<NetworkObjectSyncMessage>.Singleton.Serialize(packet, networkObj, false);
                        SendPacket(packet, PacketType.NetworkObjectSync, PacketChannel.Buffered, true, false);
                        packet.Recycle();
                    }
                }

                Processed = 5;
                while (ReloadedLobby.NetworkTransport!.IsP2PPacketAvailable(out uint messageSize, PacketChannel.Rpc))
                {
                    if (Processed <= 0) break;
                    ReadPacket(messageSize, PacketChannel.Rpc);
                    Processed--;
                }

                Processed = 5;
                while (ReloadedLobby.NetworkTransport!.IsP2PPacketAvailable(out uint messageSize, PacketChannel.Main))
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
                ReplantedOnlineMod.Logger.Error(typeof(NetworkManager), $"Exception in CoListening: {ex}");
                ReloadedLobby.LeaveLobby(() =>
                {
                    CustomPopupPanel.Show("Error", "An error occurred while processing network packets.");
                });
                Heartbeat.Dispose();
                ListeningToken = null;
                yield break;
            }

            yield return null;
        }

        ReplantedOnlineMod.Logger.Msg(typeof(NetworkManager), "Stoping...");
        Heartbeat.Dispose();

        ListeningToken = null;
    }

    /// <summary>
    /// Reads and processes a single P2P packet from the specified network channel.
    /// </summary>
    /// <param name="messageSize">The size of the incoming message in bytes.</param>
    /// <param name="channel">The network channel from which the packet was received.</param>
    private static void ReadPacket(uint messageSize, PacketChannel channel)
    {
        var buffer = PacketBuffer.Get(messageSize);

        try
        {
            if (ReloadedLobby.NetworkTransport!.ReadP2PPacket(buffer, channel))
            {
                ReloadedClientData? sender = buffer.ClientId.GetClientData();

                if (buffer.Size > 0)
                {
                    if (buffer.Data == null)
                    {
                        return;
                    }

                    ProcessPacketData(sender, buffer.Data);
                }
                else
                {
                    ReplantedOnlineMod.Logger.Error(typeof(NetworkManager), "Received packet with zero size");
                }
            }
            else
            {
                ReplantedOnlineMod.Logger.Error(typeof(NetworkManager), "Failed to read P2P packet from network buffer");
            }
        }
        finally
        {
            buffer.Recycle();
        }
    }

    /// <summary>
    /// Processes raw packet data by parsing it into subpackets and dispatching them through the streamline system.
    /// </summary>
    /// <param name="sender">The client data representing the sender of the packet.</param>
    /// <param name="data">The raw byte array containing the packet data to process.</param>
    internal static void ProcessPacketData(ReloadedClientData? sender, byte[] data)
    {
        var packetReader = PacketReader.Get(data);
        try
        {
            while (packetReader.NextSubpacket())
            {
                Streamline(sender, packetReader, false);
            }
        }
        finally
        {
            packetReader.Recycle();
        }
    }

    /// <summary>
    /// Processes an incoming packet based on its tag and routes it to the appropriate handler.
    /// </summary>
    /// <param name="sender">The client that sent the packet.</param>
    /// <param name="packetReader">The packet reader containing the packet data.</param>
    /// <param name="local">Whether if this packet is from the local client.</param>
    internal static void Streamline(ReloadedClientData? sender, PacketReader packetReader, bool local)
    {
        PacketType packetType = (PacketType)packetReader.SubpacketTag;

        if (sender == null)
        {
            ReplantedOnlineMod.Logger.Warning(typeof(NetworkManager), $"Can not processing {packetType} packet, sender client is null...");
            return;
        }

        var packetMessage = RegisterPacket.GetInstanceFromLookup(packetType);
        if (packetMessage != null)
        {
            if (RegisterPacket.TryGetAttributeFromLookup(packetMessage, out var attr))
            {
                if (attr.LogOnReceive)
                {
                    if (!local)
                    {
                        ReplantedOnlineMod.Logger.Msg(typeof(NetworkManager), $"Processing {packetType} packet from {sender.Name}");
                    }
                }
            }

            packetMessage.Receive(sender, packetReader, local);
        }
        else
        {
            ReplantedOnlineMod.Logger.Warning(typeof(NetworkManager), $"Unknown packet tag: {packetType}");
        }
    }
}
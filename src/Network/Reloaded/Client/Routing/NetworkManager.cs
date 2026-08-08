using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
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
/// Manages network communication, packet routing, and synchronization of network objects.
/// </summary>
internal static partial class NetworkManager
{
    /// <summary>
    /// Creates and initializes a new packet writer for the specified packet type.
    /// </summary>
    /// <param name="packetType">The type of packet to create.</param>
    /// <returns>A new PacketWriter instance with the subpacket started.</returns>
    internal static PacketWriter StartPacket(PacketType packetType)
    {
        PacketWriter packetWriter = PacketWriter.Get();
        packetWriter.StartSubpacket((byte)packetType);
        return packetWriter;
    }

    /// <summary>
    /// Starts a subpacket in an existing packet writer.
    /// </summary>
    /// <param name="packetWriter">The packet writer to add the subpacket to.</param>
    /// <param name="packetType">The type of packet to start.</param>
    internal static void StartPacket(PacketWriter packetWriter, PacketType packetType)
    {
        packetWriter.StartSubpacket((byte)packetType);
    }

    /// <summary>
    /// Ends the current subpacket in the packet writer.
    /// </summary>
    /// <param name="packetWriter">The packet writer containing the subpacket to end.</param>
    internal static void EndPacket(PacketWriter packetWriter)
    {
        packetWriter.EndSubpacket();
    }

    /// <summary>
    /// Ends the current subpacket and sends the packet to all clients except those specified.
    /// </summary>
    /// <param name="packetWriter">The packet writer containing the completed packet.</param>
    /// <param name="packetChannel">The channel to send the packet on.</param>
    /// <param name="log">Whether to log the send operation.</param>
    /// <param name="receiveLocally">Whether the local client should receive the packet.</param>
    /// <param name="ignoredClientIds">Client IDs to exclude from receiving the packet.</param>
    internal static void EndPacketAndSend(PacketWriter packetWriter, PacketChannel packetChannel, bool log, bool receiveLocally, params ID[] ignoredClientIds)
    {
        packetWriter.EndSubpacket();
        Send(packetWriter, packetChannel, log, receiveLocally, ignoredClientIds);
        packetWriter.Recycle();
    }

    /// <summary>
    /// Ends the current subpacket and sends the packet to a specific client.
    /// </summary>
    /// <param name="targetId">The ID of the target client.</param>
    /// <param name="packetWriter">The packet writer containing the completed packet.</param>
    /// <param name="packetChannel">The channel to send the packet on.</param>
    /// <param name="log">Whether to log the send operation.</param>
    internal static void EndPacketAndSendTo(ID targetId, PacketWriter packetWriter, PacketChannel packetChannel, bool log)
    {
        packetWriter.EndSubpacket();
        SendTo(targetId, packetWriter, packetChannel, log);
        packetWriter.Recycle();
    }

    /// <summary>
    /// Sends a packet to all clients in the lobby except those specified.
    /// </summary>
    /// <param name="packetWriter">The packet writer containing the packet to send.</param>
    /// <param name="packetChannel">The channel to send the packet on.</param>
    /// <param name="log">Whether to log the send operation.</param>
    /// <param name="receiveLocally">Whether the local client should receive the packet.</param>
    /// <param name="ignoredClientIds">Client IDs to exclude from receiving the packet.</param>
    internal static void Send(PacketWriter packetWriter, PacketChannel packetChannel, bool log, bool receiveLocally, params ID[] ignoredClientIds)
    {
        foreach (var client in ReloadedLobby.LobbyData!.AllClients.Values)
        {
            if (ignoredClientIds.Contains(client.ClientId)) continue;
            if (client.AmLocal && !receiveLocally) continue;

            if (ReloadedLobby.IsPlayerInOurLobby(client.ClientId))
            {
                SendTo(client.ClientId, packetWriter, packetChannel, log);
            }
        }
    }

    /// <summary>
    /// Sends a packet to a specific client.
    /// </summary>
    /// <param name="targetId">The ID of the target client.</param>
    /// <param name="packetWriter">The packet writer containing the packet to send.</param>
    /// <param name="packetChannel">The channel to send the packet on.</param>
    /// <param name="log">Whether to log the send operation.</param>
    internal static void SendTo(ID targetId, PacketWriter packetWriter, PacketChannel packetChannel, bool log)
    {
        if (!targetId.TryGetClientData(out var client))
        {
            return;
        }

        if (client.AmLocal == true)
        {
            ProcessPacketData(ReloadedClientData.LocalClient, packetWriter.GetBytes());
            return;
        }

        if (ReloadedLobby.IsPlayerInOurLobby(targetId))
        {
            var sendType = packetChannel is PacketChannel.Buffered ? P2PSend.ReliableWithBuffering : P2PSend.Reliable;
            ReloadedLobby.NetworkTransport!.SendP2PPacket(targetId, packetWriter.GetBytes(), packetChannel, sendType);
        }

        if (log)
        {
            ReplantedOnlineMod.Logger.Msg(typeof(NetworkManager), $"Sent to {client.Name} -> Size: {packetWriter.Length} bytes");
        }
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

                if (ReloadedLobby.LobbyData != null && ReloadedLobby.LobbyData.DirtyNetworkObjects.Count > 0)
                {
                    PacketWriter dirtyPacket = PacketWriter.Get();
                    try
                    {
                        foreach (var networkObj in ReloadedLobby.LobbyData.DirtyNetworkObjects)
                        {
                            if (networkObj == null)
                                continue;

                            if (!networkObj.AmOwner || !networkObj.IsOnNetwork || !networkObj.IsDirty)
                                continue;

                            StartPacket(dirtyPacket, PacketType.NetworkObjectSync);
                            Message<NetworkObjectSyncMessage>.Singleton.Serialize(dirtyPacket, networkObj, false);
                            EndPacket(dirtyPacket);
                        }
                        Send(dirtyPacket, PacketChannel.Buffered, false, false);
                        ReloadedLobby.LobbyData.DirtyNetworkObjects.Clear();
                    }
                    finally
                    {
                        dirtyPacket.Recycle();
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
            while (packetReader.Remaining > 0)
            {
                var subReader = packetReader.NextSubpacket();
                try
                {
                    Streamline(sender, subReader, false);
                }
                finally
                {
                    subReader.Recycle();
                }
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
        PacketType packetType = (PacketType)packetReader.SubTag;

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
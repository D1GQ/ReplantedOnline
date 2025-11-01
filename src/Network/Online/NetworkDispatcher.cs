using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Helper;
using ReplantedOnline.Items.Enums;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.RPC;

namespace ReplantedOnline.Network.Online;

/// <summary>
/// Handles network packet dispatching and reception for ReplantedOnline.
/// Manages sending packets to connected clients and processing incoming packets via RPC system.
/// </summary>
internal class NetworkDispatcher
{
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
            if (client.AmLocal && !receiveLocally) continue;

            if (NetLobby.IsPlayerInOurLobby(client.SteamId))
            {
                bool sent = SteamNetworking.SendP2PPacket(client.SteamId, packet.GetBytes(), packet.Length);
                if (sent) sentCount++;
            }
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
    internal static void SendRpc(RpcType rpc, PacketWriter packetWriter, bool receiveLocally = false)
    {
        MelonLogger.Msg($"[NetworkDispatcher] Sending RPC: {rpc}");
        var packet = PacketWriter.Get();
        packet.WriteByte((byte)rpc);
        packet.WritePacket(packetWriter);

        Send(packet, receiveLocally, PacketTag.Rpc);

        packetWriter.Recycle();
        packet.Recycle();
    }

    /// <summary>
    /// Processes all available incoming P2P packets.
    /// Called regularly to handle network communication.
    /// </summary>
    internal static void Update()
    {
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
}
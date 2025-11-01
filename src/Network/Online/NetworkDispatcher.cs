using Il2CppReloaded.Gameplay;
using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Items.Enums;
using ReplantedOnline.Network.Packet;

namespace ReplantedOnline.Network.Online;

/// <summary>
/// Handles network packet dispatching and reception for ReplantedOnline.
/// Manages sending packets to connected clients and processing incoming packets via RPC system.
/// </summary>
internal class NetworkDispatcher
{
    /// <summary>
    /// Sends a packet to all connected clients in the lobby.
    /// </summary>
    /// <param name="packetWriter">The packet writer containing the data to send.</param>
    /// <param name="receiveLocally">Whether the local client should also process this packet.</param>
    /// <param name="tag">The packet tag identifying the packet type.</param>
    internal static void Send(PacketWriter packetWriter, bool receiveLocally, PacketTag tag = Items.Enums.PacketTag.None)
    {
        var packet = PacketWriter.Get();
        packet.AddTag(tag);
        packet.WritePacket(packetWriter);

        foreach (var client in SteamNetClient.AllClients)
        {
            if (client.IsLocal && !receiveLocally) continue;

            if (NetLobby.IsPlayerInOurLobby(client.SteamId))
            {
                SteamNetworking.SendP2PPacket(client.SteamId, packet.GetBytes(), packet.Length);
            }
        }

        MelonLogger.Msg($"NetworkDispatcher: Sending Packet -> Size = {packet.Length}");

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
                var sender = SteamNetClient.GetBySteamId(buffer.Steamid);
                MelonLogger.Msg($"NetworkDispatcher: Received Packet from {sender?.Name ?? "Unknown"} -> Size = {buffer.Size}");

                if (buffer.Size > 0)
                {
                    var receivedData = buffer.ToByteArray();
                    var packetReader = PacketReader.Get(receivedData);
                    Streamline(sender, packetReader);
                }
                else
                {
                    MelonLogger.Error("NetworkDispatcher: Received Packet with zero size");
                }
            }
            else
            {
                MelonLogger.Error("NetworkDispatcher: Failed to read P2P packet");
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

        switch (tag)
        {
            case PacketTag.None:
                break;
            case PacketTag.P2P:
                MelonLogger.Msg($"NetworkDispatcher: P2P session established!");
                break;
            case PacketTag.Rpc:
                StreamlineRpc(sender, packetReader);
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
        MelonLogger.Msg($"NetworkDispatcher: Received Rpc from {sender.Name}: {Enum.GetName(rpc)}");

        switch (rpc)
        {
            case RpcType.StartGame:
                var selectionSet = (SelectionSet)packetReader.ReadByte();
                RPC.HandleGameStart(sender, selectionSet);
                break;
            case RpcType.UpdateGameState:
                var state = (GameState)packetReader.ReadByte();
                RPC.HandleUpdateGameState(sender, state);
                break;
        }
    }
}
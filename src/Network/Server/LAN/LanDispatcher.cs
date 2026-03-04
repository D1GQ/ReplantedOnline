using MelonLoader;
using ReplantedOnline.Enums;
using ReplantedOnline.Enums.LAN;
using ReplantedOnline.Monos;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Server.Packet;
using ReplantedOnline.Network.Server.Transport;
using ReplantedOnline.Structs;

namespace ReplantedOnline.Network.Server.LAN;

/// <summary>
/// Handles dispatching and processing of LAN-specific network packets.
/// Manages handshakes, client information, lobby data, and member data synchronization.
/// </summary>
internal static class LanDispatcher
{
    /// <summary>
    /// Broadcasts an internal LAN packet to all connected clients except specified exclusions.
    /// </summary>
    /// <param name="type">The type of LAN packet to broadcast.</param>
    /// <param name="writeContent">Action to write packet-specific content.</param>
    /// <param name="excludeSelf">Whether to exclude the local client.</param>
    /// <param name="excludeClient">Specific client ID to exclude from broadcast.</param>
    internal static void BroadcastInternalPacket(LanPacketType type, Action<PacketWriter> writeContent, bool excludeSelf, ID excludeClient)
    {
        if (NetLobby.NetworkTransport is LanTransport transport)
        {
            foreach (var client in transport.Clients.Values)
            {
                if (excludeSelf && client.ClientId == transport.LocalClientId) continue;
                if (!excludeClient.IsNull && client.ClientId == excludeClient) continue;
                if (client.EndPoint == null) continue;
                if (transport.ConnectionStates.TryGetValue(client.ClientId, out var state) && state != LanTransport.ConnectionState.Connected) continue;

                try
                {
                    SendInternalPacket(client.ClientId, type, writeContent);
                }
                catch { }
            }
        }
    }

    /// <summary>
    /// Sends an internal LAN packet to a specific target client.
    /// </summary>
    /// <param name="targetId">The ID of the target client.</param>
    /// <param name="type">The type of LAN packet to send.</param>
    /// <param name="writeContent">Action to write packet-specific content.</param>
    internal static void SendInternalPacket(ID targetId, LanPacketType type, Action<PacketWriter> writeContent)
    {
        if (NetLobby.NetworkTransport is LanTransport transport)
        {
            var endpoint = transport.GetClientEndpoint(targetId);
            if (endpoint == null)
            {
                MelonLogger.Warning($"[LAN] No endpoint for {targetId}");
                return;
            }

            var writer = PacketWriter.Get();
            try
            {
                writer.AddTag(PacketTag.LAN);
                writer.WriteID(NetLobby.NetworkTransport.LocalClientId);
                writer.WriteByte((byte)type);

                writeContent?.Invoke(writer);

                var packetData = writer.GetBytes();
                transport.P2PListener.Send(packetData, packetData.Length, endpoint);
            }
            finally
            {
                writer.Recycle();
            }
        }
    }

    /// <summary>
    /// Sends a handshake packet to a target client.
    /// </summary>
    /// <param name="targetId">The ID of the target client.</param>
    /// <param name="type">The type of handshake to send.</param>
    internal static void SendHandshake(ID targetId, LanHandshakeType type)
    {
        SendInternalPacket(targetId, LanPacketType.Handshake, writer =>
        {
            writer.WriteByte((byte)type);
        });
    }

    /// <summary>
    /// Sends client information to a target client.
    /// </summary>
    /// <param name="targetId">The ID of the target client.</param>
    internal static void SendClientInfo(ID targetId)
    {
        if (NetLobby.NetworkTransport is LanTransport transport)
        {
            SendInternalPacket(targetId, LanPacketType.ClientInfo, writer =>
            {
                var clientInfo = new ClientInfo
                {
                    ClientId = transport.LocalClientId,
                    Name = transport.CurrentLobbyData.Name,
                    EndPoint = null
                };
                clientInfo.Serialize(writer);
            });
        }
    }

    /// <summary>
    /// Sends lobby data to a target client.
    /// </summary>
    /// <param name="targetId">The ID of the target client.</param>
    /// <param name="key">The lobby data key.</param>
    /// <param name="value">The lobby data value.</param>
    internal static void SendLobbyData(ID targetId, string key, string value)
    {
        SendInternalPacket(targetId, LanPacketType.LobbyData, writer =>
        {
            writer.WriteString(key);
            writer.WriteString(value);
        });
    }

    /// <summary>
    /// Sends member data to a target client.
    /// </summary>
    /// <param name="targetId">The ID of the target client.</param>
    /// <param name="memberId">The ID of the member whose data is being sent.</param>
    /// <param name="key">The member data key.</param>
    /// <param name="value">The member data value.</param>
    internal static void SendMemberData(ID targetId, ID memberId, string key, string value)
    {
        SendInternalPacket(targetId, LanPacketType.MemberData, writer =>
        {
            writer.WriteID(memberId);
            writer.WriteString(key);
            writer.WriteString(value);
        });
    }

    /// <summary>
    /// Sends all existing lobby and member data to a newly connected client.
    /// </summary>
    /// <param name="clientId">The ID of the client to send data to.</param>
    /// <param name="transport">The LAN transport instance.</param>
    internal static void SendExistingDataToClient(ID clientId, LanTransport transport)
    {
        // Send lobby data
        if (transport.LobbyData.TryGetValue(transport.CurrentLobbyId, out var lobbyData))
        {
            foreach (var kvp in lobbyData)
            {
                SendLobbyData(clientId, kvp.Key, kvp.Value);
            }
        }

        // Send member data
        if (transport.MemberData.TryGetValue(transport.CurrentLobbyId, out var members))
        {
            foreach (var member in members)
            {
                if (member.Key == clientId) continue;
                foreach (var data in member.Value)
                {
                    SendMemberData(clientId, member.Key, data.Key, data.Value);
                }
            }
        }
    }

    /// <summary>
    /// Handles an incoming LAN packet.
    /// </summary>
    /// <param name="senderId">The ID of the sender.</param>
    /// <param name="reader">The packet reader containing the packet data.</param>
    /// <param name="transport">The LAN transport instance.</param>
    internal static void HandleLanPacket(ID senderId, PacketReader reader, LanTransport transport)
    {
        var internalType = (LanPacketType)reader.ReadByte();

        switch (internalType)
        {
            case LanPacketType.Handshake:
                var handshakeType = (LanHandshakeType)reader.ReadByte();
                HandleHandshake(senderId, handshakeType, transport);
                break;

            case LanPacketType.ClientInfo:
                HandleClientInfo(senderId, reader, transport);
                break;

            case LanPacketType.LobbyData:
                HandleLobbyData(senderId, reader, transport);
                break;

            case LanPacketType.MemberData:
                HandleMemberData(senderId, reader, transport);
                break;
        }
    }

    /// <summary>
    /// Handles an incoming RPC packet by queueing it for processing.
    /// </summary>
    /// <param name="senderId">The ID of the sender.</param>
    /// <param name="reader">The packet reader containing the RPC data.</param>
    /// <param name="transport">The LAN transport instance.</param>
    internal static void HandleRPCPacket(ID senderId, PacketReader reader, LanTransport transport)
    {
        try
        {
            var rpcData = reader.ReadBytes();

            lock (transport.PacketQueue)
            {
                transport.PacketQueue.Enqueue(new PendingPacket
                {
                    Data = rpcData,
                    SenderId = senderId,
                    Size = (uint)rpcData.Length
                });
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[LAN] Error handling RPC: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles handshake packets for connection establishment and management.
    /// </summary>
    /// <param name="senderId">The ID of the sender.</param>
    /// <param name="type">The type of handshake.</param>
    /// <param name="transport">The LAN transport instance.</param>
    internal static void HandleHandshake(ID senderId, LanHandshakeType type, LanTransport transport)
    {
        switch (type)
        {
            case LanHandshakeType.Request:
                lock (transport._lock)
                {
                    // Auto-accept if we're host and in a lobby
                    if (transport.IsHost && transport.CurrentLobbyId.HasValue)
                    {
                        MelonLogger.Msg($"[LAN] Auto-accepting connection from {senderId}");

                        // Initialize connection state
                        transport.ConnectionStates[senderId] = LanTransport.ConnectionState.Connected;

                        // Send acceptance
                        SendHandshake(senderId, LanHandshakeType.Accept);

                        // Send our info
                        SendClientInfo(senderId);

                        // Send existing lobby data
                        SendExistingDataToClient(senderId, transport);

                        // Notify game of new member
                        MainThreadDispatcher.Execute(() =>
                        {
                            NetLobby.OnLobbyMemberJoined(transport.CurrentLobbyData, senderId);
                        });
                    }
                    else if (!transport.PendingRequests.Contains(senderId))
                    {
                        transport.PendingRequests.Add(senderId);
                        transport.ConnectionStates[senderId] = LanTransport.ConnectionState.Handshaking;
                        MelonLogger.Msg($"[LAN] Handshake request from {senderId}");
                        MainThreadDispatcher.Execute(() =>
                        {
                            NetLobby.OnP2PSessionRequest(senderId);
                        });
                    }
                }
                break;

            case LanHandshakeType.Accept:
                MelonLogger.Msg($"[LAN] Handshake accepted by {senderId}");
                lock (transport._lock)
                {
                    transport.ConnectionStates[senderId] = LanTransport.ConnectionState.Connected;

                    if (!transport.IsHost && transport.CurrentLobbyId.HasValue)
                    {
                        // Send our info to host
                        SendClientInfo(senderId);

                        // Complete the join process
                        transport.HandshakeCompletionSource?.TrySetResult(true);
                    }
                }
                break;

            case LanHandshakeType.Leave:
                MelonLogger.Msg($"[LAN] Client left: {senderId}");
                transport.RemoveClient(senderId);
                break;
        }
    }

    /// <summary>
    /// Handles incoming client information packets.
    /// </summary>
    /// <param name="senderId">The ID of the sender.</param>
    /// <param name="reader">The packet reader containing client info.</param>
    /// <param name="transport">The LAN transport instance.</param>
    internal static void HandleClientInfo(ID senderId, PacketReader reader, LanTransport transport)
    {
        try
        {
            var clientInfo = new ClientInfo();
            clientInfo.Deserialize(reader);

            MelonLogger.Msg($"[LAN] Client info: {clientInfo.Name} ({clientInfo.ClientId})");

            lock (transport._lock)
            {
                // Get the actual endpoint from our clients dictionary
                if (transport.Clients.TryGetValue(senderId, out var existingClient))
                {
                    clientInfo.EndPoint = existingClient.EndPoint;
                }

                transport.AddClient(clientInfo.ClientId, clientInfo.EndPoint, clientInfo.Name);
                transport.PendingRequests.Remove(clientInfo.ClientId);
                transport.ConnectionStates[clientInfo.ClientId] = LanTransport.ConnectionState.Connected;

                // Initialize member data
                if (transport.CurrentLobbyId.HasValue)
                {
                    if (!transport.MemberData.ContainsKey(transport.CurrentLobbyId))
                        transport.MemberData[transport.CurrentLobbyId] = [];

                    if (!transport.MemberData[transport.CurrentLobbyId].ContainsKey(clientInfo.ClientId))
                    {
                        transport.MemberData[transport.CurrentLobbyId][clientInfo.ClientId] = [];

                        // If we're host, notify others
                        if (transport.IsHost)
                        {
                            BroadcastInternalPacket(LanPacketType.ClientInfo,
                                writer => new ClientInfo { ClientId = clientInfo.ClientId, Name = clientInfo.Name }.Serialize(writer),
                                true, clientInfo.ClientId);

                            MainThreadDispatcher.Execute(() =>
                            {
                                NetLobby.OnLobbyMemberJoined(transport.CurrentLobbyData, clientInfo.ClientId);
                            });
                        }
                    }
                }

                // If we're client and this is the host, complete handshake
                if (!transport.IsHost && clientInfo.ClientId == transport.GetLobbyOwner(transport.CurrentLobbyId))
                {
                    transport.HandshakeCompletionSource?.TrySetResult(true);
                }
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[LAN] Error handling client info: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles incoming lobby data packets.
    /// </summary>
    /// <param name="senderId">The ID of the sender.</param>
    /// <param name="reader">The packet reader containing lobby data.</param>
    /// <param name="transport">The LAN transport instance.</param>
    internal static void HandleLobbyData(ID senderId, PacketReader reader, LanTransport transport)
    {
        if (!transport.CurrentLobbyId.HasValue) return;

        var key = reader.ReadString();
        var value = reader.ReadString();

        lock (transport._lock)
        {
            if (!transport.LobbyData.ContainsKey(transport.CurrentLobbyId))
                transport.LobbyData[transport.CurrentLobbyId] = [];

            transport.LobbyData[transport.CurrentLobbyId][key] = value;

            // Update the LobbyData struct if needed
            transport.UpdateLobbyDataStruct(key, value);

            if (!transport.IsHost)
            {
                MainThreadDispatcher.Execute(() =>
                {
                    NetLobby.OnLobbyDataChanged(transport.CurrentLobbyData);
                });
            }
        }
    }

    /// <summary>
    /// Handles incoming member data packets.
    /// </summary>
    /// <param name="senderId">The ID of the sender.</param>
    /// <param name="reader">The packet reader containing member data.</param>
    /// <param name="transport">The LAN transport instance.</param>
    internal static void HandleMemberData(ID senderId, PacketReader reader, LanTransport transport)
    {
        if (!transport.CurrentLobbyId.HasValue) return;

        var targetId = reader.ReadID();
        var key = reader.ReadString();
        var value = reader.ReadString();

        lock (transport._lock)
        {
            if (!transport.MemberData.ContainsKey(transport.CurrentLobbyId))
                transport.MemberData[transport.CurrentLobbyId] = [];
            if (!transport.MemberData[transport.CurrentLobbyId].ContainsKey(targetId))
                transport.MemberData[transport.CurrentLobbyId][targetId] = [];

            transport.MemberData[transport.CurrentLobbyId][targetId][key] = value;
        }

        // If host, broadcast to others
        if (transport.IsHost)
        {
            BroadcastInternalPacket(LanPacketType.MemberData,
                writer =>
                {
                    writer.WriteID(targetId);
                    writer.WriteString(key);
                    writer.WriteString(value);
                },
                true, senderId);
        }
    }
}
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
                ReplantedOnlineMod.Logger.Warning($"[LAN] No endpoint for {targetId}");
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
    /// For requests, includes the client's information.
    /// For accept/leave, just sends the type.
    /// </summary>
    /// <param name="targetId">The ID of the target client.</param>
    /// <param name="type">The type of handshake to send.</param>
    /// <param name="clientInfo">Optional client info for request handshakes.</param>
    internal static void SendHandshake(ID targetId, LanHandshakeType type, ClientInfo clientInfo = null)
    {
        SendInternalPacket(targetId, LanPacketType.Handshake, writer =>
        {
            writer.WriteByte((byte)type);

            // Include client info with request handshake
            if (type == LanHandshakeType.Request && clientInfo != null)
            {
                clientInfo.Serialize(writer);
            }
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
                    Name = transport.PlayerName,
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
    /// Also sends information about all existing clients so the new client knows everyone.
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

        // Send member data for ALL existing clients (including host)
        if (transport.MemberData.TryGetValue(transport.CurrentLobbyId, out var members))
        {
            foreach (var member in members)
            {
                if (member.Key == clientId) continue; // Don't send the new client their own data

                // Send this member's data to the new client
                foreach (var data in member.Value)
                {
                    SendMemberData(clientId, member.Key, data.Key, data.Value);
                }
            }
        }

        // Also send all client info so the new client knows everyone's names
        foreach (var client in transport.Clients.Values)
        {
            if (client.ClientId == clientId || client.ClientId == transport.LocalClientId) continue;

            SendInternalPacket(clientId, LanPacketType.ClientInfo, writer =>
            {
                new ClientInfo
                {
                    ClientId = client.ClientId,
                    Name = client.Name
                }.Serialize(writer);
            });
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

                // For request handshakes, read the included client info
                ClientInfo clientInfo = null;
                if (handshakeType == LanHandshakeType.Request && reader.Remaining > 0)
                {
                    clientInfo = new ClientInfo();
                    clientInfo.Deserialize(reader);
                }

                HandleHandshake(senderId, handshakeType, clientInfo, transport);
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
            ReplantedOnlineMod.Logger.Error($"[LAN] Error handling RPC: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles handshake packets for connection establishment and management.
    /// Host is the source of truth for all client information.
    /// Client sends their info with the initial request.
    /// </summary>
    /// <param name="senderId">The ID of the sender.</param>
    /// <param name="type">The type of handshake.</param>
    /// <param name="clientInfo">The client's information (for request handshakes).</param>
    /// <param name="transport">The LAN transport instance.</param>
    internal static void HandleHandshake(ID senderId, LanHandshakeType type, ClientInfo clientInfo, LanTransport transport)
    {
        switch (type)
        {
            case LanHandshakeType.Request:
                lock (transport._lock)
                {
                    // Only host should handle connection requests
                    if (transport.IsHost && transport.CurrentLobbyId.HasValue)
                    {
                        if (clientInfo == null)
                        {
                            ReplantedOnlineMod.Logger.Error($"[LAN] Received handshake request without client info from {senderId}");
                            return;
                        }

                        ReplantedOnlineMod.Logger.Msg($"[LAN] Host processing connection request from {clientInfo.Name} ({senderId})");

                        // Get endpoint for this client
                        var endpoint = transport.GetClientEndpoint(senderId);

                        // Add client to host's dictionaries with their REAL name
                        transport.AddClient(clientInfo.ClientId, endpoint, clientInfo.Name);
                        transport.ConnectionStates[clientInfo.ClientId] = LanTransport.ConnectionState.Connected;

                        // Initialize member data for new client
                        if (!transport.MemberData.ContainsKey(transport.CurrentLobbyId))
                            transport.MemberData[transport.CurrentLobbyId] = [];

                        if (!transport.MemberData[transport.CurrentLobbyId].ContainsKey(clientInfo.ClientId))
                            transport.MemberData[transport.CurrentLobbyId][clientInfo.ClientId] = [];

                        // Send acceptance back to client
                        SendHandshake(clientInfo.ClientId, LanHandshakeType.Accept);

                        // Send host's info to the new client
                        SendClientInfo(clientInfo.ClientId);

                        // Send existing lobby data to new client
                        SendExistingDataToClient(clientInfo.ClientId, transport);

                        // Broadcast this new client's info to ALL other connected clients
                        foreach (var otherClient in transport.Clients.Values.Where(c =>
                            c.ClientId != transport.LocalClientId &&
                            c.ClientId != clientInfo.ClientId &&
                            c.EndPoint != null))
                        {
                            SendInternalPacket(otherClient.ClientId, LanPacketType.ClientInfo, writer =>
                            {
                                new ClientInfo
                                {
                                    ClientId = clientInfo.ClientId,
                                    Name = clientInfo.Name
                                }.Serialize(writer);
                            });
                        }

                        // Notify game of new member
                        MainThreadDispatcher.Execute(() =>
                        {
                            NetLobby.OnLobbyMemberJoined(transport.CurrentLobbyData, clientInfo.ClientId);
                        });
                    }
                    else if (!transport.PendingRequests.Contains(senderId))
                    {
                        transport.PendingRequests.Add(senderId);
                        transport.ConnectionStates[senderId] = LanTransport.ConnectionState.Handshaking;
                        ReplantedOnlineMod.Logger.Msg($"[LAN] Handshake request from {senderId}");

                        MainThreadDispatcher.Execute(() =>
                        {
                            NetLobby.OnP2PSessionRequest(senderId);
                        });
                    }
                }
                break;

            case LanHandshakeType.Accept:
                ReplantedOnlineMod.Logger.Msg($"[LAN] Handshake accepted by {senderId}");
                lock (transport._lock)
                {
                    transport.ConnectionStates[senderId] = LanTransport.ConnectionState.Connected;

                    if (!transport.IsHost && transport.CurrentLobbyId.HasValue)
                    {
                        // We're a client who got accepted by host
                        // Complete the join process
                        transport.HandshakeCompletionSource?.TrySetResult(true);
                    }
                }
                break;

            case LanHandshakeType.Leave:
                ReplantedOnlineMod.Logger.Msg($"[LAN] Client left: {senderId}");

                if (transport.IsHost)
                {
                    // Host broadcasts to all other clients that this client left
                    foreach (var client in transport.Clients.Values.Where(c =>
                        c.ClientId != transport.LocalClientId &&
                        c.ClientId != senderId &&
                        c.EndPoint != null))
                    {
                        SendHandshake(client.ClientId, LanHandshakeType.Leave);
                    }
                }

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

            ReplantedOnlineMod.Logger.Msg($"[LAN] Received client info: {clientInfo.Name} ({clientInfo.ClientId})");

            lock (transport._lock)
            {
                // If we're a client receiving info about another client
                if (!transport.IsHost && transport.CurrentLobbyId.HasValue)
                {
                    // Add or update client in our local dictionary
                    transport.AddClient(clientInfo.ClientId, null, clientInfo.Name);

                    // Initialize member data if needed
                    if (!transport.MemberData.ContainsKey(transport.CurrentLobbyId))
                        transport.MemberData[transport.CurrentLobbyId] = [];

                    if (!transport.MemberData[transport.CurrentLobbyId].ContainsKey(clientInfo.ClientId))
                    {
                        transport.MemberData[transport.CurrentLobbyId][clientInfo.ClientId] = [];

                        // Notify game of new member
                        MainThreadDispatcher.Execute(() =>
                        {
                            NetLobby.OnLobbyMemberJoined(transport.CurrentLobbyData, clientInfo.ClientId);
                        });
                    }
                }

                // If we're client and this is the host's info, we might already have it from handshake
                if (!transport.IsHost && clientInfo.ClientId == transport.GetLobbyOwner(transport.CurrentLobbyId))
                {
                    // Just update the name if needed
                    if (transport.Clients.TryGetValue(clientInfo.ClientId, out var existingClient))
                    {
                        existingClient.Name = clientInfo.Name;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error($"[LAN] Error handling client info: {ex.Message}");
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
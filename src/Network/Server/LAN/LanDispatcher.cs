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
    /// For accept/reject, includes optional reason.
    /// </summary>
    internal static void SendHandshake(ID targetId, LanHandshakeType type, string reason = null, ClientInfo clientInfo = null)
    {
        SendInternalPacket(targetId, LanPacketType.Handshake, writer =>
        {
            writer.WriteByte((byte)type);

            switch (type)
            {
                case LanHandshakeType.Request when clientInfo != null:
                    clientInfo.Serialize(writer);
                    break;

                case LanHandshakeType.Reject when !string.IsNullOrEmpty(reason):
                    writer.WriteString(reason);
                    break;
            }
        });
    }

    /// <summary>
    /// Sends client information to a target client.
    /// </summary>
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
    internal static void HandleLanPacket(ID senderId, PacketReader reader, LanTransport transport)
    {
        var internalType = (LanPacketType)reader.ReadByte();

        switch (internalType)
        {
            case LanPacketType.Handshake:
                HandleHandshake(senderId, reader, transport);
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
    internal static void HandleRPCPacket(ID senderId, PacketReader reader, LanTransport transport)
    {
        try
        {
            var channel = reader.ReadInt();
            var rpcData = reader.ReadBytes();

            lock (transport.PacketQueue)
            {
                if (transport.PacketQueue.TryGetValue((PacketChannel)channel, out var queue))
                {
                    queue.Enqueue(new PendingPacket
                    {
                        Data = rpcData,
                        SenderId = senderId,
                        Size = (uint)rpcData.Length
                    });
                }
            }
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error($"[LAN] Error handling RPC: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles handshake packets for connection establishment and management.
    /// </summary>
    internal static void HandleHandshake(ID senderId, PacketReader reader, LanTransport transport)
    {
        var type = (LanHandshakeType)reader.ReadByte();

        switch (type)
        {
            case LanHandshakeType.Request:
                HandleHandshakeRequest(senderId, reader, transport);
                break;

            case LanHandshakeType.Accept:
                HandleHandshakeAccept(senderId, transport);
                break;

            case LanHandshakeType.Reject:
                HandleHandshakeReject(senderId, reader, transport);
                break;

            case LanHandshakeType.Leave:
                HandleHandshakeLeave(senderId, transport);
                break;
        }
    }

    private static void HandleHandshakeRequest(ID senderId, PacketReader reader, LanTransport transport)
    {
        lock (transport.SyncLock)
        {
            // Read client info if present
            ClientInfo clientInfo = null;
            if (reader.Remaining > 0)
            {
                clientInfo = new ClientInfo();
                clientInfo.Deserialize(reader);
            }

            // Only host should handle connection requests
            if (transport.IsHost && transport.CurrentLobbyId.HasValue)
            {
                ProcessHostConnectionRequest(senderId, clientInfo, transport);
            }
            else if (!transport.PendingRequests.Contains(senderId))
            {
                ProcessClientConnectionRequest(senderId, transport);
            }
        }
    }

    private static void ProcessHostConnectionRequest(ID senderId, ClientInfo clientInfo, LanTransport transport)
    {
        if (clientInfo == null)
        {
            ReplantedOnlineMod.Logger.Error($"[LAN] Received handshake request without client info from {senderId}");
            return;
        }

        // Check if lobby is joinable
        if (!transport.CurrentLobbyData.IsJoinable)
        {
            ReplantedOnlineMod.Logger.Msg($"[LAN] Rejecting connection from {clientInfo.Name} - lobby is not joinable");
            SendHandshake(clientInfo.ClientId, LanHandshakeType.Reject, "Lobby is not joinable");
            return;
        }

        // Check if lobby is full
        if (transport.GetNumLobbyMembers(transport.CurrentLobbyId) >= transport.CurrentLobbyData.MaxPlayers)
        {
            ReplantedOnlineMod.Logger.Msg($"[LAN] Rejecting connection from {clientInfo.Name} - lobby is full");
            SendHandshake(clientInfo.ClientId, LanHandshakeType.Reject, "Lobby is full");
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

    private static void ProcessClientConnectionRequest(ID senderId, LanTransport transport)
    {
        transport.PendingRequests.Add(senderId);
        transport.ConnectionStates[senderId] = LanTransport.ConnectionState.Handshaking;
        ReplantedOnlineMod.Logger.Msg($"[LAN] Handshake request from {senderId}");

        MainThreadDispatcher.Execute(() =>
        {
            NetLobby.OnP2PSessionRequest(senderId);
        });
    }

    private static void HandleHandshakeAccept(ID senderId, LanTransport transport)
    {
        ReplantedOnlineMod.Logger.Msg($"[LAN] Handshake accepted by {senderId}");

        lock (transport.SyncLock)
        {
            transport.ConnectionStates[senderId] = LanTransport.ConnectionState.Connected;

            if (!transport.IsHost && transport.CurrentLobbyId.HasValue)
            {
                // We're a client who got accepted by host
                transport.HandshakeCompletionSource?.TrySetResult(true);
            }
        }
    }

    private static void HandleHandshakeReject(ID senderId, PacketReader reader, LanTransport transport)
    {
        string reason = reader.Remaining > 0 ? reader.ReadString() : "Connection rejected";
        ReplantedOnlineMod.Logger.Msg($"[LAN] Handshake rejected by {senderId}: {reason}");

        lock (transport.SyncLock)
        {
            transport.ConnectionStates[senderId] = LanTransport.ConnectionState.Rejected;
            transport.RejectionReasons[senderId] = reason;

            // Complete the handshake with failure
            transport.HandshakeCompletionSource?.TrySetResult(false);

            // Clean up
            transport.PendingRequests.Remove(senderId);
            transport.Clients.Remove(senderId);
        }
    }

    private static void HandleHandshakeLeave(ID senderId, LanTransport transport)
    {
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
    }

    /// <summary>
    /// Handles incoming client information packets.
    /// </summary>
    internal static void HandleClientInfo(ID senderId, PacketReader reader, LanTransport transport)
    {
        try
        {
            var clientInfo = new ClientInfo();
            clientInfo.Deserialize(reader);

            ReplantedOnlineMod.Logger.Msg($"[LAN] Received client info: {clientInfo.Name} ({clientInfo.ClientId})");

            lock (transport.SyncLock)
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
    internal static void HandleLobbyData(ID senderId, PacketReader reader, LanTransport transport)
    {
        if (!transport.CurrentLobbyId.HasValue) return;

        var key = reader.ReadString();
        var value = reader.ReadString();

        lock (transport.SyncLock)
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
    internal static void HandleMemberData(ID senderId, PacketReader reader, LanTransport transport)
    {
        if (!transport.CurrentLobbyId.HasValue) return;

        var targetId = reader.ReadID();
        var key = reader.ReadString();
        var value = reader.ReadString();

        lock (transport.SyncLock)
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
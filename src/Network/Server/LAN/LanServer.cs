using Il2CppSteamworks;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Managers;
using ReplantedOnline.Monos;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Structs;
using System.Net;
using System.Net.Sockets;

namespace ReplantedOnline.Network.Server.LAN;

/// <summary>
/// Main LAN server implementation that handles both hosting and client connections.
/// Manages P2P networking, lobby creation, client synchronization, and packet routing.
/// </summary>
internal sealed class LanServer : IDisposable
{
    /// <summary>
    /// Event fired when a lobby creation operation completes.
    /// </summary>
    internal static event Action<Result, ServerLobby> OnLobbyCreatedCompleted;

    /// <summary>
    /// Event fired when successfully entering a lobby.
    /// </summary>
    internal static event Action<ServerLobby> OnLobbyEnteredCompleted;

    /// <summary>
    /// Event fired when lobby data is modified.
    /// </summary>
    internal static event Action<ServerLobby> OnLobbyDataChanged;

    /// <summary>
    /// Event fired when a new member joins the lobby.
    /// </summary>
    internal static event Action<ServerLobby, ID> OnLobbyMemberJoined;

    /// <summary>
    /// Event fired when a member leaves the lobby.
    /// </summary>
    internal static event Action<ServerLobby, ID> OnLobbyMemberLeave;

    /// <summary>
    /// Packet types used for server/client communication.
    /// </summary>
    internal enum ServerPacket
    {
        HandshakeRequest,
        HandshakeAccept,
        HandshakeReject,
        HandshakeLeave,
        SyncClients,
        LobbyData,
        LobbyDataUpdate,
        MemberData,
        Rpc,
    }

    private readonly object _stateLock = new();
    private readonly object _clientsLock = new();

    /// <summary>
    /// Gets the singleton server instance.
    /// </summary>
    internal static LanServer Server { get; set; }

    /// <summary>
    /// Server data containing lobby information and metadata.
    /// </summary>
    internal LanServerData ServerData;

    /// <summary>
    /// Broadcast service for LAN discovery.
    /// </summary>
    internal readonly LanServerBroadcast ServerBroadcast;

    /// <summary>
    /// UDP client for P2P communication.
    /// </summary>
    internal UdpClient P2PClient;

    /// <summary>
    /// Cancellation token source for async operations.
    /// </summary>
    internal CancellationTokenSource P2PCTS;

    /// <summary>
    /// Packet queues organized by channel type.
    /// </summary>
    internal readonly Dictionary<PacketChannel, Queue<PendingPacket>> PacketQueue = [];

    /// <summary>
    /// Local client's unique identifier.
    /// </summary>
    internal ID LocalClientId;

    /// <summary>
    /// Dictionary of all connected clients.
    /// </summary>
    internal readonly Dictionary<ID, LanServerClientData> Clients = [];

    /// <summary>
    /// Set of client IDs with pending connection requests.
    /// </summary>
    internal readonly HashSet<ID> PendingRequests = [];

    /// <summary>
    /// Rejection reasons for failed connection attempts.
    /// </summary>
    internal readonly Dictionary<ID, string> RejectionReasons = [];

    /// <summary>
    /// Name of the local player.
    /// </summary>
    internal string LocalPlayerName = "Player";

    /// <summary>
    /// Indicates whether this instance is acting as a host.
    /// </summary>
    internal bool IsHost = false;

    /// <summary>
    /// Indicates whether the client is connected to a lobby.
    /// </summary>
    internal bool IsConnected = false;

    /// <summary>
    /// Task completion source for handshake operations.
    /// </summary>
    internal TaskCompletionSource<bool> HandshakeCompletionSource;

    private bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the LanServer class.
    /// </summary>
    internal LanServer()
    {
        ServerData = new();
        ServerBroadcast = new(this);
        foreach (var channel in Enum.GetValues<PacketChannel>())
        {
            PacketQueue[channel] = [];
        }
    }

    /// <summary>
    /// Starts a new lobby as a host.
    /// </summary>
    /// <param name="playerName">The name of the host player.</param>
    /// <param name="maxPlayers">Maximum number of players allowed in the lobby.</param>
    internal static void StartHost(string playerName, int maxPlayers)
    {
        lock (Server._stateLock)
        {
            if (Server._isRunning) return;
            Server._isRunning = true;
            Server.LocalPlayerName = playerName;
            Server.IsHost = true;
            Server.IsConnected = true;
        }

        Server.P2PCTS?.Dispose();
        Server.P2PCTS = new();
        Server.ServerData.Reset();
        Server.ServerData.LobbyId = ID.CreateRandomULong();

        Server.P2PClient = new();
        Server.P2PClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        Server.P2PClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

        var localEndpoint = (IPEndPoint)Server.P2PClient.Client.LocalEndPoint;
        var localIP = GetLocalNetworkIP();

        Server.ServerData.HostAddress = localIP;
        Server.ServerData.GamePort = localEndpoint.Port;
        Server.ServerData.SetServerName($"{playerName}'s Lobby");
        Server.ServerData.SetMaxPlayerCount(maxPlayers);
        Server.ServerData.SetPlayerCount(1);
        Server.ServerData.SetIsJoinable(true);
        Server.ServerData.SetModVersion(ModInfo.MOD_VERSION_FORMATTED);
        Server.ServerData.SetGameCode(MatchmakingManager.GenerateGameCode(Server.ServerData.LobbyId));

        Task.Run(Server.ListenForP2P, Server.P2PCTS.Token);

        var actualLocalEndpoint = new IPEndPoint(localIP, localEndpoint.Port);
        Server.LocalClientId = new ID(actualLocalEndpoint, Enums.IdType.IPEndPoint);
        Server.ServerData.HostId = Server.LocalClientId;

        Server.CreateClient(Server.LocalClientId, actualLocalEndpoint, playerName);
        Server.ServerBroadcast.StartBroadcasting();

        MainThreadDispatcher.Execute(() =>
        {
            if (Server == null || !Server._isRunning) return;
            OnLobbyCreatedCompleted?.Invoke(Result.OK, Server.ServerData.ToServerLobby());
            OnLobbyEnteredCompleted?.Invoke(Server.ServerData.ToServerLobby());
        });
    }

    /// <summary>
    /// Initializes the client for joining a lobby.
    /// </summary>
    /// <param name="playerName">The name of the local player.</param>
    internal static void StartClient(string playerName)
    {
        lock (Server._stateLock)
        {
            if (Server._isRunning) return;
            Server._isRunning = true;
            Server.LocalPlayerName = playerName;
            Server.IsHost = false;
            Server.IsConnected = false;
        }

        Server.P2PCTS?.Dispose();
        Server.P2PCTS = new();
        Server.ServerData.Reset();
        Server.P2PClient = new();
        Server.P2PClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        Server.P2PClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        Server.ServerData.GamePort = ((IPEndPoint)Server.P2PClient.Client.LocalEndPoint).Port;

        Task.Run(Server.ListenForP2P, Server.P2PCTS.Token);

        var localEndpoint = (IPEndPoint)Server.P2PClient.Client.LocalEndPoint;
        var localIP = GetLocalNetworkIP();
        var actualLocalEndpoint = new IPEndPoint(localIP, localEndpoint.Port);
        Server.LocalClientId = new ID(actualLocalEndpoint, Enums.IdType.IPEndPoint);
    }

    /// <summary>
    /// Sends a join request to a discovered server.
    /// </summary>
    /// <param name="serverData">The server data of the lobby to join.</param>
    internal void JoinServer(LanServerData serverData)
    {
        lock (_stateLock)
        {
            ServerData = serverData;
            IsHost = false;
            HandshakeCompletionSource = new TaskCompletionSource<bool>();
        }

        if (serverData.HostAddress == null)
        {
            ReplantedOnlineMod.Logger.Error("[Server] No host address in server data");
            return;
        }

        var hostEndpoint = new IPEndPoint(serverData.HostAddress, serverData.GamePort);
        var writer = PacketWriter.Get();
        LanServerProtocol.SerializeHandshakeRequest(writer, LocalPlayerName, LocalClientId);
        SendServerPacketTo(hostEndpoint, writer, ServerPacket.HandshakeRequest);
        writer.Recycle();
    }

    /// <summary>
    /// Disconnects from the current lobby and cleans up resources.
    /// </summary>
    internal static void Leave()
    {
        if (Server == null) return;

        Server.ServerBroadcast.StopBroadcasting();
        Server.P2PCTS.Cancel();

        bool shouldRun;
        lock (Server._stateLock)
        {
            shouldRun = Server._isRunning;
            Server._isRunning = false;
        }

        if (!shouldRun) return;

        if (Server.IsHost)
        {
            var clientsCopy = Server.GetOtherClients();
            foreach (var client in clientsCopy)
            {
                var writer = PacketWriter.Get();
                Server.SendServerPacketTo(client.ClientId.AsIPEndPoint(), writer, ServerPacket.HandshakeLeave);
                writer.Recycle();
            }
        }
        else
        {
            bool isConnected;
            ID hostId;
            lock (Server._stateLock)
            {
                isConnected = Server.IsConnected;
                hostId = Server.ServerData.HostId;
            }

            if (hostId != ID.Null && isConnected)
            {
                var writer = PacketWriter.Get();
                Server.SendServerPacketTo(hostId.AsIPEndPoint(), writer, ServerPacket.HandshakeLeave);
                writer.Recycle();
            }
        }

        Server.P2PClient.Close();
        Server.ServerData.Reset();

        lock (Server._clientsLock) Server.Clients.Clear();
        lock (Server._stateLock)
        {
            Server.PendingRequests.Clear();
            Server.IsConnected = false;
        }
    }

    /// <summary>
    /// Listens for incoming P2P packets asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ListenForP2P()
    {
        while (!P2PCTS.Token.IsCancellationRequested)
        {
            try
            {
                var result = await P2PClient.ReceiveAsync();
                ProcessServerPacket(result.Buffer, result.RemoteEndPoint);
            }
            catch (OperationCanceledException) { break; }
            catch (SocketException ex) when (
                ex.SocketErrorCode == SocketError.OperationAborted ||
                ex.SocketErrorCode == SocketError.Interrupted ||
                ex.SocketErrorCode == SocketError.ConnectionReset)
            { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex)
            {
                ReplantedOnlineMod.Logger.Error($"[Server] P2P listen error: {ex}");
                await Task.Delay(100, P2PCTS.Token);
            }
        }
    }

    /// <summary>
    /// Creates a new client entry and adds it to the client dictionary.
    /// </summary>
    /// <param name="clientId">The unique identifier for the client.</param>
    /// <param name="iPEndPoint">The network endpoint of the client.</param>
    /// <param name="playerName">The display name of the player.</param>
    /// <returns>The created client data object.</returns>
    internal LanServerClientData CreateClient(ID clientId, IPEndPoint iPEndPoint, string playerName)
    {
        var client = new LanServerClientData
        {
            PlayerName = playerName,
            ClientId = clientId
        };

        lock (_clientsLock) Clients[clientId] = client;
        return client;
    }

    /// <summary>
    /// Removes a client from the connected clients dictionary and updates lobby state.
    /// </summary>
    /// <param name="client">The client data to remove.</param>
    internal void RemoveClient(LanServerClientData client)
    {
        lock (_clientsLock) Clients.Remove(client.ClientId);
        lock (_stateLock)
        {
            PendingRequests.Remove(client.ClientId);
            RejectionReasons.Remove(client.ClientId);
        }

        if (IsHost)
        {
            lock (_clientsLock) ServerData.SetPlayerCount(Clients.Count);
            BroadcastSyncClients();
        }
    }

    /// <summary>
    /// Sends a packet to all connected clients except the local client.
    /// </summary>
    /// <param name="packetWriter">The packet writer containing the packet data.</param>
    /// <param name="serverPacket">The type of server packet being sent.</param>
    internal void SendServerPacket(PacketWriter packetWriter, ServerPacket serverPacket)
    {
        var clientsCopy = GetOtherClients();
        foreach (var client in clientsCopy)
        {
            SendServerPacketTo(client.ClientId.AsIPEndPoint(), packetWriter, serverPacket);
        }
    }

    /// <summary>
    /// Sends a packet to a specific network endpoint.
    /// </summary>
    /// <param name="iPEndPoint">The destination endpoint.</param>
    /// <param name="packetWriter">The packet writer containing the packet data.</param>
    /// <param name="serverPacket">The type of server packet being sent.</param>
    internal void SendServerPacketTo(IPEndPoint iPEndPoint, PacketWriter packetWriter, ServerPacket serverPacket)
    {
        if (iPEndPoint == null) return;

        var writer = PacketWriter.Get();
        writer.AddTag(PacketHandlerType.Server);
        writer.WriteEnum(serverPacket);
        writer.WritePacketToBuffer(packetWriter);
        var buffer = writer.GetByteBuffer();
        writer.Recycle();

        try
        {
            P2PClient.Send(buffer, buffer.Length, iPEndPoint);
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error($"[Server] Failed to send packet to {iPEndPoint}: {ex}");
        }
    }

    /// <summary>
    /// Broadcasts the current client list to all connected clients.
    /// </summary>
    internal void BroadcastSyncClients()
    {
        var writer = PacketWriter.Get();
        lock (_clientsLock) LanServerProtocol.SerializeSyncClients(writer, Clients);

        var clientsCopy = GetOtherClients();
        foreach (var client in clientsCopy)
        {
            SendServerPacketTo(client.ClientId.AsIPEndPoint(), writer, ServerPacket.SyncClients);
        }
        writer.Recycle();
    }

    /// <summary>
    /// Sends a P2P packet to a specific client.
    /// </summary>
    /// <param name="clientId">The ID of the target client.</param>
    /// <param name="data">The packet data to send.</param>
    /// <param name="channel">The channel to send the packet on.</param>
    /// <returns>True if the packet was sent successfully, false otherwise.</returns>
    internal bool SendP2PPacket(ID clientId, byte[] data, PacketChannel channel)
    {
        if (clientId == LocalClientId)
        {
            lock (PacketQueue) PacketQueue[channel].Enqueue(new PendingPacket
            {
                Data = data,
                SenderId = LocalClientId,
                Size = (uint)data.Length
            });
            return true;
        }

        lock (_clientsLock)
        {
            if (!Clients.ContainsKey(clientId))
            {
                ReplantedOnlineMod.Logger.Warning($"[Server] Cannot send RPC to {clientId} - client not found");
                return false;
            }
        }

        var endpoint = clientId.AsIPEndPoint();
        if (endpoint == null) return false;

        var writer = PacketWriter.Get();
        LanServerProtocol.SerializeRPC(writer, channel, data);
        SendServerPacketTo(endpoint, writer, ServerPacket.Rpc);
        writer.Recycle();
        return true;
    }

    /// <summary>
    /// Sets a key-value pair in the lobby data. Only callable by the host.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <param name="value">The data value.</param>
    /// <param name="remove">If true, removes the key instead of setting it.</param>
    internal void SetLobbyData(string key, string value, bool remove = false)
    {
        if (!IsHost) return;

        if (remove) ServerData.Data.Remove(key);
        else ServerData.Data[key] = value;

        MainThreadDispatcher.Execute(() =>
        {
            if (Server != null && Server._isRunning)
                OnLobbyDataChanged?.Invoke(ServerData.ToServerLobby());
        });

        var writer = PacketWriter.Get();
        LanServerProtocol.SerializeSetLobbyData(writer, key, value, remove);
        SendServerPacket(writer, ServerPacket.LobbyDataUpdate);
        writer.Recycle();
    }

    /// <summary>
    /// Requests to set member data for the local client.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <param name="value">The data value.</param>
    internal void SetMemberData(string key, string value)
    {
        lock (_clientsLock)
        {
            if (Clients.TryGetValue(LocalClientId, out var client))
                client.Data[key] = value;
        }

        var writer = PacketWriter.Get();
        LanServerProtocol.SerializeMemberData(writer, key, value);
        SendServerPacket(writer, ServerPacket.MemberData);
        writer.Recycle();
    }

    /// <summary>
    /// Processes an incoming server packet based on its type.
    /// </summary>
    /// <param name="buffer">The raw packet data.</param>
    /// <param name="remoteEndPoint">The endpoint the packet came from.</param>
    private void ProcessServerPacket(byte[] buffer, IPEndPoint remoteEndPoint)
    {
        var packetReader = PacketReader.Get(buffer);
        var senderId = new ID(remoteEndPoint, Enums.IdType.IPEndPoint);

        if (packetReader.GetTag() != PacketHandlerType.Server) return;

        var serverPacket = packetReader.ReadEnum<ServerPacket>();

        switch (serverPacket)
        {
            case ServerPacket.HandshakeRequest: ProcessHandshakeRequest(senderId, remoteEndPoint, packetReader); break;
            case ServerPacket.HandshakeAccept: ProcessHandshakeAccept(senderId, packetReader); break;
            case ServerPacket.HandshakeReject: ProcessHandshakeReject(senderId, packetReader); break;
            case ServerPacket.HandshakeLeave: ProcessHandshakeLeave(senderId); break;
            case ServerPacket.SyncClients: ProcessSyncClients(senderId, packetReader); break;
            case ServerPacket.Rpc: ProcessRPC(senderId, packetReader); break;
            case ServerPacket.LobbyData: ProcessLobbyData(senderId, packetReader); break;
            case ServerPacket.LobbyDataUpdate: ProcessLobbyDataUpdate(senderId, packetReader); break;
            case ServerPacket.MemberData: ProcessMemberData(senderId, packetReader); break;
        }

        packetReader.Recycle();
    }

    /// <summary>
    /// Processes a handshake request from a potential client.
    /// </summary>
    /// <param name="senderId">The ID of the requesting client.</param>
    /// <param name="endpoint">The network endpoint of the client.</param>
    /// <param name="reader">The packet reader containing the request data.</param>
    private void ProcessHandshakeRequest(ID senderId, IPEndPoint endpoint, PacketReader reader)
    {
        if (!IsHost) return;

        var (clientName, clientId) = LanServerProtocol.DeserializeHandshakeRequest(reader);

        if (!ServerData.GetIsJoinable())
        {
            SendRejection(endpoint, "Lobby is not joinable");
            return;
        }

        lock (_clientsLock)
        {
            if (Clients.Count >= ServerData.GetMaxPlayerCount())
            {
                SendRejection(endpoint, "Lobby is full");
                return;
            }
        }

        var clientData = CreateClient(clientId, endpoint, clientName);
        lock (_clientsLock) ServerData.SetPlayerCount(Clients.Count);

        MainThreadDispatcher.Execute(() =>
        {
            if (Server != null && Server._isRunning)
                OnLobbyMemberJoined?.Invoke(ServerData.ToServerLobby(), clientData.ClientId);
        });

        var acceptWriter = PacketWriter.Get();
        LanServerProtocol.SerializeHandshakeAccept(acceptWriter, ServerData.LobbyId);
        SendServerPacketTo(endpoint, acceptWriter, ServerPacket.HandshakeAccept);
        acceptWriter.Recycle();

        var lobbyWriter = PacketWriter.Get();
        LanServerProtocol.SerializeLobbyData(lobbyWriter, ServerData);
        SendServerPacketTo(endpoint, lobbyWriter, ServerPacket.LobbyData);
        lobbyWriter.Recycle();

        var syncWriter = PacketWriter.Get();
        lock (_clientsLock) LanServerProtocol.SerializeSyncClients(syncWriter, Clients);
        SendServerPacketTo(endpoint, syncWriter, ServerPacket.SyncClients);
        syncWriter.Recycle();

        BroadcastSyncClients();
    }

    /// <summary>
    /// Processes a handshake acceptance from a host.
    /// </summary>
    /// <param name="senderId">The ID of the host.</param>
    /// <param name="reader">The packet reader containing the acceptance data.</param>
    private void ProcessHandshakeAccept(ID senderId, PacketReader reader)
    {
        if (IsHost) return;

        var lobbyId = LanServerProtocol.DeserializeHandshakeAccept(reader);
        lock (_stateLock)
        {
            ServerData.LobbyId = lobbyId;
            IsConnected = true;
            HandshakeCompletionSource?.TrySetResult(true);
        }

        MainThreadDispatcher.Execute(() =>
        {
            if (Server != null && Server._isRunning)
                OnLobbyEnteredCompleted?.Invoke(ServerData.ToServerLobby());
        });
    }

    /// <summary>
    /// Processes a handshake rejection from a host.
    /// </summary>
    /// <param name="senderId">The ID of the host.</param>
    /// <param name="reader">The packet reader containing the rejection reason.</param>
    private void ProcessHandshakeReject(ID senderId, PacketReader reader)
    {
        var reason = LanServerProtocol.DeserializeHandshakeReject(reader);
        lock (_stateLock)
        {
            RejectionReasons[senderId] = reason;
            HandshakeCompletionSource?.TrySetResult(false);
        }
    }

    /// <summary>
    /// Processes a leave notification from a client.
    /// </summary>
    /// <param name="senderId">The ID of the leaving client.</param>
    private void ProcessHandshakeLeave(ID senderId)
    {
        lock (_clientsLock)
        {
            if (Clients.TryGetValue(senderId, out var leavingClient))
            {
                RemoveClient(leavingClient);
                MainThreadDispatcher.Execute(() =>
                {
                    if (Server != null && Server._isRunning)
                        OnLobbyMemberLeave?.Invoke(ServerData.ToServerLobby(), senderId);
                });
            }
        }
    }

    /// <summary>
    /// Processes a client synchronization packet from the host.
    /// </summary>
    /// <param name="senderId">The ID of the host.</param>
    /// <param name="reader">The packet reader containing the client list.</param>
    private void ProcessSyncClients(ID senderId, PacketReader reader)
    {
        if (IsHost) return;

        var syncedClients = LanServerProtocol.DeserializeSyncClients(reader);

        Dictionary<ID, LanServerClientData> oldClients;
        lock (_clientsLock) oldClients = new Dictionary<ID, LanServerClientData>(Clients);

        var newClients = syncedClients.Where(c => !oldClients.ContainsKey(c.Key)).Select(c => c.Value).ToList();
        var removedClients = oldClients.Where(c => !syncedClients.ContainsKey(c.Key)).Select(c => c.Value).ToList();

        lock (_clientsLock)
        {
            Clients.Clear();
            foreach (var client in syncedClients) Clients[client.Key] = client.Value;
        }

        ServerData.SetPlayerCount(Clients.Count);

        MainThreadDispatcher.Execute(() =>
        {
            if (Server == null || !Server._isRunning) return;

            var lobbySnapshot = ServerData.ToServerLobby();
            foreach (var newClient in newClients) OnLobbyMemberJoined?.Invoke(lobbySnapshot, newClient.ClientId);
            foreach (var removedClient in removedClients) OnLobbyMemberLeave?.Invoke(lobbySnapshot, removedClient.ClientId);
        });
    }

    /// <summary>
    /// Processes an RPC packet and adds it to the appropriate channel queue.
    /// </summary>
    /// <param name="senderId">The ID of the client that sent the RPC.</param>
    /// <param name="reader">The packet reader containing the RPC data.</param>
    private void ProcessRPC(ID senderId, PacketReader reader)
    {
        var (channel, data) = LanServerProtocol.DeserializeRPC(reader);
        lock (PacketQueue)
        {
            PacketQueue[channel].Enqueue(new PendingPacket
            {
                Data = data,
                SenderId = senderId,
                Size = (uint)data.Length
            });
        }
    }

    /// <summary>
    /// Processes initial lobby data from the host.
    /// </summary>
    /// <param name="senderId">The ID of the host.</param>
    /// <param name="reader">The packet reader containing the lobby data.</param>
    private void ProcessLobbyData(ID senderId, PacketReader reader)
    {
        if (!IsHost) LanServerProtocol.DeserializeLobbyData(reader, ServerData);
    }

    /// <summary>
    /// Processes a lobby data update from the host.
    /// </summary>
    /// <param name="senderId">The ID of the host.</param>
    /// <param name="reader">The packet reader containing the updated lobby data.</param>
    private void ProcessLobbyDataUpdate(ID senderId, PacketReader reader)
    {
        if (IsHost) return;

        var (key, value, remove) = LanServerProtocol.DeserializeSetLobbyData(reader);
        if (remove) ServerData.Data.Remove(key);
        else ServerData.Data[key] = value;

        MainThreadDispatcher.Execute(() =>
        {
            if (Server != null && Server._isRunning)
                OnLobbyDataChanged?.Invoke(ServerData.ToServerLobby());
        });
    }

    /// <summary>
    /// Processes member data from the host.
    /// </summary>
    /// <param name="senderId">The ID of the host.</param>
    /// <param name="reader">The packet reader containing the member data.</param>
    private void ProcessMemberData(ID senderId, PacketReader reader)
    {
        var (key, value) = LanServerProtocol.DeserializeMemberData(reader);
        lock (_clientsLock)
        {
            if (Clients.TryGetValue(senderId, out var client))
                client.Data[key] = value;
        }
    }

    /// <summary>
    /// Reads the next available P2P packet from the specified channel queue.
    /// </summary>
    /// <param name="buffer">The buffer to store the packet data in.</param>
    /// <param name="channel">The channel to read from.</param>
    /// <returns>True if a packet was available, false otherwise.</returns>
    internal bool ReadP2PPacket(P2PPacketBuffer buffer, PacketChannel channel)
    {
        lock (PacketQueue)
        {
            if (PacketQueue.TryGetValue(channel, out var queue) && queue.Count > 0)
            {
                var packet = queue.Dequeue();
                buffer.Data = packet.Data;
                buffer.Size = packet.Size;
                buffer.ClientId = packet.SenderId;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if a P2P packet is available on the specified channel.
    /// </summary>
    /// <param name="msgSize">Output parameter that receives the size of the next packet.</param>
    /// <param name="channel">The channel to check.</param>
    /// <returns>True if a packet is available, false otherwise.</returns>
    internal bool IsP2PPacketAvailable(ref uint msgSize, PacketChannel channel)
    {
        lock (PacketQueue)
        {
            if (PacketQueue.TryGetValue(channel, out var queue) && queue.Count > 0)
            {
                msgSize = queue.Peek().Size;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Sends a rejection response to a client.
    /// </summary>
    /// <param name="endpoint">The endpoint to send the rejection to.</param>
    /// <param name="reason">The reason for rejection.</param>
    private void SendRejection(IPEndPoint endpoint, string reason)
    {
        var writer = PacketWriter.Get();
        LanServerProtocol.SerializeHandshakeReject(writer, reason);
        SendServerPacketTo(endpoint, writer, ServerPacket.HandshakeReject);
        writer.Recycle();
    }

    /// <summary>
    /// Gets a list of all connected clients except the local client.
    /// </summary>
    /// <returns>A list of client data objects.</returns>
    private List<LanServerClientData> GetOtherClients()
    {
        lock (_clientsLock)
        {
            return Clients.Values.Where(c => c.ClientId != LocalClientId).ToList();
        }
    }

    /// <summary>
    /// Gets the local network IP address.
    /// </summary>
    /// <returns>The local IPv4 address.</returns>
    /// <exception cref="Exception">Thrown when no suitable network IP is found.</exception>
    private static IPAddress GetLocalNetworkIP()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                return ip;
        }
        throw new Exception("Error getting LocalNetworkIP!");
    }

    /// <summary>
    /// Disposes of all resources used by the LanServer.
    /// </summary>
    public void Dispose()
    {
        ServerData?.Dispose();
        ServerBroadcast?.Dispose();
        P2PCTS?.Dispose();
        P2PClient?.Dispose();
        Server = null;
    }

    /// <summary>
    /// Represents a pending packet waiting to be processed.
    /// </summary>
    internal sealed class PendingPacket
    {
        /// <summary>
        /// The raw packet data.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// The ID of the client that sent the packet.
        /// </summary>
        public ID SenderId { get; set; }

        /// <summary>
        /// The size of the packet in bytes.
        /// </summary>
        public uint Size { get; set; }
    }
}
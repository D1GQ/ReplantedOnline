using Il2CppSteamworks;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Managers;
using ReplantedOnline.Monos;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Structs;
using ReplantedOnline.Utilities;
using System.Net;
using System.Net.Sockets;

namespace ReplantedOnline.Network.Server.LAN;

/// <summary>
/// Main LAN server implementation that handles both hosting and member connections.
/// Manages P2P networking, lobby creation, member synchronization, and packet routing.
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
    /// Packet types used for server/member communication.
    /// </summary>
    internal enum ServerPacket
    {
        HandshakeRequest,
        HandshakeAccept,
        HandshakeReject,
        HandshakeLeave,
        SyncMembers,
        LobbyData,
        LobbyDataUpdate,
        MemberData,
        Rpc,
    }

    private readonly object _stateLock = new();
    private readonly object _membersLock = new();

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
    /// Local member's unique identifier.
    /// </summary>
    internal ID LocalMemberId;

    /// <summary>
    /// Dictionary of all connected members.
    /// </summary>
    internal readonly Dictionary<ID, LanMemberData> Members = [];

    /// <summary>
    /// Set of member IDs with pending connection requests.
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
    /// Indicates whether the member is connected to a lobby.
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
        Server.LocalMemberId = new ID(actualLocalEndpoint, Enums.IdType.IPEndPoint);
        Server.ServerData.HostId = Server.LocalMemberId;

        Server.CreateMember(Server.LocalMemberId, actualLocalEndpoint, playerName);
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
        Server.LocalMemberId = new ID(actualLocalEndpoint, Enums.IdType.IPEndPoint);
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
            ReplantedOnlineMod.Logger.Error(typeof(LanServer), "No host address in server data");
            return;
        }

        var hostEndpoint = new IPEndPoint(serverData.HostAddress, serverData.GamePort);
        var writer = PacketWriter.Get();
        LanServerProtocol.SerializeHandshakeRequest(writer, LocalPlayerName, LocalMemberId);
        SendServerPacketTo(hostEndpoint, writer, ServerPacket.HandshakeRequest);
        writer.Recycle();
    }

    /// <summary>
    /// Disconnects from the current lobby and cleans up resources.
    /// </summary>
    internal static void Leave()
    {
        if (Server == null) return;

        bool shouldRun;
        lock (Server._stateLock)
        {
            shouldRun = Server._isRunning;
            Server._isRunning = false;
        }

        if (!shouldRun) return;

        Server.ServerBroadcast.StopBroadcasting();
        Server.P2PCTS.Cancel();

        if (Server.IsHost)
        {
            foreach (var member in Server.GetOtherMembers())
            {
                var writer = PacketWriter.Get();
                Server.SendServerPacketTo(member.MemberId.AsIPEndPoint(), writer, ServerPacket.HandshakeLeave);
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

        lock (Server._membersLock) Server.Members.Clear();
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
                ReplantedOnlineMod.Logger.Error(typeof(LanServer), $"P2P listen error: {ex}");
                await Task.Delay(100, P2PCTS.Token);
            }
        }
    }

    /// <summary>
    /// Creates a new member entry and adds it to the member dictionary.
    /// </summary>
    /// <param name="memberId">The unique identifier for the member.</param>
    /// <param name="iPEndPoint">The network endpoint of the member.</param>
    /// <param name="playerName">The display name of the player.</param>
    /// <returns>The created member data object.</returns>
    internal LanMemberData CreateMember(ID memberId, IPEndPoint iPEndPoint, string playerName)
    {
        var member = new LanMemberData
        {
            PlayerName = playerName,
            MemberId = memberId
        };

        lock (_membersLock) Members[memberId] = member;
        return member;
    }

    /// <summary>
    /// Removes a member from the connected members dictionary and updates lobby state.
    /// </summary>
    /// <param name="member">The member data to remove.</param>
    internal void RemoveMember(LanMemberData member)
    {
        lock (_membersLock) Members.Remove(member.MemberId);
        lock (_stateLock)
        {
            PendingRequests.Remove(member.MemberId);
            RejectionReasons.Remove(member.MemberId);
        }

        if (IsHost)
        {
            lock (_membersLock) ServerData.SetPlayerCount(Members.Count);
            BroadcastSyncMembers();
        }
    }

    /// <summary>
    /// Sends a packet to all connected members except the local member.
    /// </summary>
    /// <param name="packetWriter">The packet writer containing the packet data.</param>
    /// <param name="serverPacket">The type of server packet being sent.</param>
    internal void SendServerPacket(PacketWriter packetWriter, ServerPacket serverPacket)
    {
        foreach (var member in GetOtherMembers())
        {
            SendServerPacketTo(member.MemberId.AsIPEndPoint(), packetWriter, serverPacket);
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
            ReplantedOnlineMod.Logger.Error(typeof(LanServer), $"Failed to send packet to {iPEndPoint}: {ex}");
        }
    }

    /// <summary>
    /// Broadcasts the current member list to all connected members.
    /// </summary>
    internal void BroadcastSyncMembers()
    {
        var writer = PacketWriter.Get();
        lock (_membersLock) LanServerProtocol.SerializeSyncMembers(writer, Members);

        foreach (var member in GetOtherMembers())
        {
            SendServerPacketTo(member.MemberId.AsIPEndPoint(), writer, ServerPacket.SyncMembers);
        }
        writer.Recycle();
    }

    /// <summary>
    /// Sends a P2P packet to a specific member.
    /// </summary>
    /// <param name="memberId">The ID of the target member.</param>
    /// <param name="data">The packet data to send.</param>
    /// <param name="channel">The channel to send the packet on.</param>
    /// <returns>True if the packet was sent successfully, false otherwise.</returns>
    internal bool SendP2PPacket(ID memberId, byte[] data, PacketChannel channel)
    {
        if (memberId == LocalMemberId)
        {
            lock (PacketQueue) PacketQueue[channel].Enqueue(new PendingPacket
            {
                Data = data,
                SenderId = LocalMemberId,
                Size = (uint)data.Length
            });
            return true;
        }

        lock (_membersLock)
        {
            if (!Members.ContainsKey(memberId))
            {
                ReplantedOnlineMod.Logger.Warning(typeof(LanServer), $"Cannot send RPC to {memberId} - member not found");
                return false;
            }
        }

        var endpoint = memberId.AsIPEndPoint();
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
    /// Requests to set member data for the local member.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <param name="value">The data value.</param>
    internal void SetMemberData(string key, string value)
    {
        lock (_membersLock)
        {
            if (Members.TryGetValue(LocalMemberId, out var member))
                member.Data[key] = value;
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
            case ServerPacket.SyncMembers: ProcessSyncMembers(senderId, packetReader); break;
            case ServerPacket.Rpc: ProcessRPC(senderId, packetReader); break;
            case ServerPacket.LobbyData: ProcessLobbyData(senderId, packetReader); break;
            case ServerPacket.LobbyDataUpdate: ProcessLobbyDataUpdate(senderId, packetReader); break;
            case ServerPacket.MemberData: ProcessMemberData(senderId, packetReader); break;
        }

        packetReader.Recycle();
    }

    /// <summary>
    /// Processes a handshake request from a potential member.
    /// </summary>
    /// <param name="senderId">The ID of the requesting member.</param>
    /// <param name="endpoint">The network endpoint of the member.</param>
    /// <param name="reader">The packet reader containing the request data.</param>
    private void ProcessHandshakeRequest(ID senderId, IPEndPoint endpoint, PacketReader reader)
    {
        if (!IsHost) return;

        var (memberName, memberId) = LanServerProtocol.DeserializeHandshakeRequest(reader);

        if (!ServerData.GetIsJoinable())
        {
            SendRejection(endpoint, "Lobby is not joinable");
            return;
        }

        lock (_membersLock)
        {
            if (Members.Count >= ServerData.GetMaxPlayerCount())
            {
                SendRejection(endpoint, "Lobby is full");
                return;
            }
        }

        var memberData = CreateMember(memberId, endpoint, memberName);
        lock (_membersLock) ServerData.SetPlayerCount(Members.Count);

        MainThreadDispatcher.Execute(() =>
        {
            if (Server != null && Server._isRunning)
                OnLobbyMemberJoined?.Invoke(ServerData.ToServerLobby(), memberData.MemberId);
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
        lock (_membersLock) LanServerProtocol.SerializeSyncMembers(syncWriter, Members);
        SendServerPacketTo(endpoint, syncWriter, ServerPacket.SyncMembers);
        syncWriter.Recycle();

        BroadcastSyncMembers();
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
    /// Processes a leave notification from a member.
    /// </summary>
    /// <param name="senderId">The ID of the leaving member.</param>
    private void ProcessHandshakeLeave(ID senderId)
    {
        lock (_membersLock)
        {
            if (Members.TryGetValue(senderId, out var leavingMember))
            {
                RemoveMember(leavingMember);
                MainThreadDispatcher.Execute(() =>
                {
                    if (Server != null && Server._isRunning)
                        OnLobbyMemberLeave?.Invoke(ServerData.ToServerLobby(), senderId);
                });
            }
        }
    }

    /// <summary>
    /// Processes a member synchronization packet from the host.
    /// </summary>
    /// <param name="senderId">The ID of the host.</param>
    /// <param name="reader">The packet reader containing the member list.</param>
    private void ProcessSyncMembers(ID senderId, PacketReader reader)
    {
        if (IsHost) return;

        var syncedMembers = LanServerProtocol.DeserializeSyncMembers(reader);

        Dictionary<ID, LanMemberData> oldMembers;
        lock (_membersLock) oldMembers = new Dictionary<ID, LanMemberData>(Members);

        var newMembers = syncedMembers.Where(c => !oldMembers.ContainsKey(c.Key)).Select(c => c.Value).ToList();
        var removedMembers = oldMembers.Where(c => !syncedMembers.ContainsKey(c.Key)).Select(c => c.Value).ToList();

        lock (_membersLock)
        {
            Members.Clear();
            foreach (var member in syncedMembers) Members[member.Key] = member.Value;
        }

        ServerData.SetPlayerCount(Members.Count);

        MainThreadDispatcher.Execute(() =>
        {
            if (Server == null || !Server._isRunning) return;

            var lobbyData = ServerData.ToServerLobby();
            foreach (var newMember in newMembers) OnLobbyMemberJoined?.Invoke(lobbyData, newMember.MemberId);
            foreach (var removedMember in removedMembers) OnLobbyMemberLeave?.Invoke(lobbyData, removedMember.MemberId);
        });
    }

    /// <summary>
    /// Processes an RPC packet and adds it to the appropriate channel queue.
    /// </summary>
    /// <param name="senderId">The ID of the member that sent the RPC.</param>
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
        lock (_membersLock)
        {
            if (Members.TryGetValue(senderId, out var member))
                member.Data[key] = value;
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

                if (packet.Data == null || packet.Data.Length != packet.Size || packet.Size == 0)
                {
                    packet.Dispose();
                    return false;
                }

                buffer.Data = packet.Data;
                buffer.Size = packet.Size;
                buffer.ClientId = packet.SenderId;
                packet.Dispose();

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
    /// Sends a rejection response to a member.
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
    /// Gets a list of all connected members except the local member.
    /// </summary>
    /// <returns>A list of member data objects.</returns>
    private List<LanMemberData> GetOtherMembers()
    {
        lock (_membersLock)
        {
            return Members.Values.Where(c => c.MemberId != LocalMemberId).ToList();
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
    }

    /// <summary>
    /// Represents a pending packet waiting to be processed.
    /// </summary>
    internal sealed class PendingPacket : IDisposable
    {
        /// <summary>
        /// The raw packet data.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// The ID of the member that sent the packet.
        /// </summary>
        public ID SenderId { get; set; }

        /// <summary>
        /// The size of the packet in bytes.
        /// </summary>
        public uint Size { get; set; }

        /// <summary>
        /// Clear the data of the PendingPacket.
        /// </summary>
        public void Dispose()
        {
            Data = null;
        }
    }
}
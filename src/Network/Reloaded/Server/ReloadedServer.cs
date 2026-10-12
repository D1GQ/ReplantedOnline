using Il2CppSteamworks;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.MonoScripts.Unity;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.MelonLoader;
using System.Net;
using System.Net.Sockets;

namespace ReplantedOnline.Network.Reloaded.Server;

/// <summary>
/// Main IP server implementation that handles both hosting and member connections.
/// Manages P2P networking, lobby creation, member synchronization, and packet routing.
/// Works over raw UDP for LAN, direct-IP, and any reachable endpoint.
/// </summary>
internal sealed class ReloadedServer : IDisposable
{
    /// <summary>Fired when a lobby creation operation completes.</summary>
    internal static event Action<Result, ServerLobby>? OnLobbyCreatedCompleted;

    /// <summary>Fired when successfully entering a lobby.</summary>
    internal static event Action<ServerLobby>? OnLobbyEnteredCompleted;

    /// <summary>Fired when lobby data is modified.</summary>
    internal static event Action<ServerLobby>? OnLobbyDataChanged;

    /// <summary>Fired when a new member joins the lobby.</summary>
    internal static event Action<ServerLobby, ID>? OnLobbyMemberJoined;

    /// <summary>Fired when a member leaves the lobby.</summary>
    internal static event Action<ServerLobby, ID>? OnLobbyMemberLeave;

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

    /// <summary>Gets the singleton server instance.</summary>
    internal static ReloadedServer Server { get; set; } = default!;

    /// <summary>Server data containing lobby information and metadata.</summary>
    internal ReloadedServerData ServerData = default!;

    /// <summary>
    /// Optional LAN broadcast service. Null when LAN discovery is disabled
    /// (e.g. direct-IP hosting or dedicated server).
    /// </summary>
    internal readonly ReloadedServerBroadcast? ServerBroadcast;

    /// <summary>UDP client for P2P communication.</summary>
    internal UdpClient? P2PClient;

    /// <summary>Cancellation token source for async operations.</summary>
    internal CancellationTokenSource P2PCTS = default!;

    /// <summary>Packet queues organized by channel type.</summary>
    internal readonly Dictionary<PacketChannel, Queue<PendingPacket>> PacketQueue = [];

    /// <summary>Local member's unique identifier.</summary>
    internal ID LocalMemberId = ID.Null;

    /// <summary>Dictionary of all connected members.</summary>
    internal readonly Dictionary<ID, ReloadedMemberData> Members = [];

    /// <summary>Set of member IDs with pending connection requests.</summary>
    internal readonly HashSet<ID> PendingRequests = [];

    /// <summary>Rejection reasons for failed connection attempts.</summary>
    internal readonly Dictionary<ID, string> RejectionReasons = [];

    /// <summary>Name of the local player.</summary>
    internal string LocalPlayerName = "Player";

    /// <summary>Indicates whether this instance is acting as a host.</summary>
    internal bool IsHost = false;

    /// <summary>Indicates whether the member is connected to a lobby.</summary>
    internal bool IsConnected = false;

    /// <summary>Describes how this peer is connected.</summary>
    internal ConnectionMode Mode = ConnectionMode.None;

    /// <summary>Task completion source for handshake operations.</summary>
    internal TaskCompletionSource<bool>? HandshakeCompletionSource;

    private static readonly IPAddress LOCALHOST_IP = IPAddress.Parse("127.0.0.1");

    private bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the IpServer class.
    /// </summary>
    /// <param name="enableBroadcast">
    /// If true, sets up LAN broadcast discovery. Set to false for direct-IP-only
    /// or dedicated server use to avoid binding port 14242.
    /// </param>
    internal ReloadedServer(bool enableBroadcast = true)
    {
        ServerData = new();
        if (enableBroadcast)
        {
            ServerBroadcast = new(this);
        }
        foreach (var channel in Enum.GetValues<PacketChannel>())
        {
            PacketQueue[channel] = [];
        }
    }

    /// <summary>
    /// Starts a new lobby as a host.
    /// </summary>
    /// <param name="maxPlayers">Maximum number of players allowed in the lobby.</param>
    /// <param name="broadcast">
    /// If true, advertises the lobby on the LAN. If false, clients must connect
    /// by explicit IP/hostname and port.
    /// </param>
    internal static void StartHost(int maxPlayers, bool broadcast = true)
    {
        string name;
        try
        {
            name = SteamFriends.Internal.GetPersonaName();
        }
        catch
        {
            name = "Host";
        }

        lock (Server._stateLock)
        {
            if (Server._isRunning)
                return;

            Server._isRunning = true;
            Server.LocalPlayerName = name;
            Server.IsHost = true;
            Server.IsConnected = true;
            Server.Mode = broadcast ? ConnectionMode.LanHost : ConnectionMode.DirectHost;
        }

        Server.P2PCTS?.Dispose();
        Server.P2PCTS = new();
        Server.ServerData.Reset();
        Server.ServerData.LobbyId = ID.CreateRandomULong();

        Server.P2PClient = new();
        Server.P2PClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        Server.P2PClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

        var localEndpoint = (IPEndPoint)Server.P2PClient.Client.LocalEndPoint!;
        var localIP = GetLocalNetworkIP();

        Server.ServerData.HostAddress = localIP;
        Server.ServerData.GamePort = localEndpoint.Port;
        Server.ServerData.SetServerName($"{name}'s Lobby");
        Server.ServerData.SetMaxPlayerCount(maxPlayers);
        Server.ServerData.SetPlayerCount(1);
        Server.ServerData.SetIsJoinable(true);
        Server.ServerData.SetModVersion(ReplantedOnlineMod.ModInfo.MOD_VERSION_FORMATTED);
        Server.ServerData.SetGameCode(ReloadedMatchmaking.GenerateGameCode(Server.ServerData.LobbyId));

        Task.Run(Server.ListenForP2P, Server.P2PCTS.Token);

        var actualLocalEndpoint = new IPEndPoint(localIP, localEndpoint.Port);
        Server.LocalMemberId = new ID(actualLocalEndpoint, IdType.IPEndPoint);
        Server.ServerData.HostId = Server.LocalMemberId;

        Server.CreateMember(Server.LocalMemberId, actualLocalEndpoint, name);

        if (broadcast)
        {
            Server.ServerBroadcast?.StartBroadcasting();
        }

        MainThreadDispatcher.Execute(() =>
        {
            if (Server == null || !Server._isRunning)
                return;

            OnLobbyCreatedCompleted?.Invoke(Result.OK, Server.ServerData.ToServerLobby());
            OnLobbyEnteredCompleted?.Invoke(Server.ServerData.ToServerLobby());
        });
    }

    /// <summary>
    /// Initializes the client for joining a lobby.
    /// </summary>
    /// <param name="mode">
    /// Whether this client will discover via LAN or connect directly by IP.
    /// Affects bookkeeping only — the handshake path is identical.
    /// </param>
    internal static void StartClient(ConnectionMode mode = ConnectionMode.LanClient)
    {
        string name;
        try
        {
            name = SteamFriends.Internal.GetPersonaName();
        }
        catch
        {
            name = "Client";
        }

        lock (Server._stateLock)
        {
            if (Server._isRunning)
                return;

            Server._isRunning = true;
            Server.LocalPlayerName = name;
            Server.IsHost = false;
            Server.IsConnected = false;
            Server.Mode = mode;
        }

        Server.P2PCTS?.Dispose();
        Server.P2PCTS = new();
        Server.ServerData.Reset();
        Server.P2PClient = new();
        Server.P2PClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        Server.P2PClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        Server.ServerData.GamePort = ((IPEndPoint)Server.P2PClient.Client.LocalEndPoint!).Port;

        Task.Run(Server.ListenForP2P, Server.P2PCTS.Token);

        var localEndpoint = (IPEndPoint)Server.P2PClient.Client.LocalEndPoint;
        var localIP = GetLocalNetworkIP();
        var actualLocalEndpoint = new IPEndPoint(localIP, localEndpoint.Port);
        Server.LocalMemberId = new ID(actualLocalEndpoint, IdType.IPEndPoint);
    }

    /// <summary>
    /// Tears down an in-progress client that never successfully joined.
    /// </summary>
    internal static void AbortClient()
    {
        if (Server == null)
            return;

        bool wasRunning;
        lock (Server._stateLock)
        {
            wasRunning = Server._isRunning;
            Server._isRunning = false;
            Server.IsConnected = false;
            Server.Mode = ConnectionMode.None;
        }

        if (!wasRunning)
            return;

        try { Server.ServerBroadcast?.StopBroadcasting(); } catch { }
        try { Server.P2PCTS?.Cancel(); } catch { }
        try { Server.P2PClient?.Close(); } catch { }

        Server.ServerData?.Reset();

        lock (Server._membersLock) Server.Members.Clear();
        lock (Server._stateLock)
        {
            Server.PendingRequests.Clear();
            Server.RejectionReasons.Clear();
            Server.HandshakeCompletionSource = null;
        }
    }

    /// <summary>
    /// Sends a join request to a discovered or explicitly-specified server.
    /// </summary>
    internal async Task<bool> JoinServer(ReloadedServerData serverData)
    {
        lock (_stateLock)
        {
            ServerData = serverData;
            IsHost = false;
            HandshakeCompletionSource = new TaskCompletionSource<bool>();
        }

        if (serverData.HostAddress == null)
        {
            ReplantedOnlineMod.Logger.Error(typeof(ReloadedServer), "No host address in server data");
            return false;
        }

        var hostEndpoint = new IPEndPoint(serverData.HostAddress, serverData.GamePort);
        var writer = PacketWriter.Get();
        writer.StartSubpacket((byte)ServerPacket.HandshakeRequest);
        ReloadedServerProtocol.SerializeHandshakeRequest(writer, LocalPlayerName, LocalMemberId);
        writer.EndSubpacket();
        SendRawPacketTo(hostEndpoint, writer);
        writer.Recycle();

        try
        {
            var timeout = Task.Delay(5000);
            var completed = await Task.WhenAny(HandshakeCompletionSource.Task, timeout);

            if (completed == timeout)
            {
                ReplantedOnlineMod.Logger.Error(typeof(ReloadedServer), "Handshake timed out");
                return false;
            }

            return await HandshakeCompletionSource.Task;
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(typeof(ReloadedServer), $"Handshake error: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Joins a server by explicit IP address or hostname and port.
    /// Performs DNS resolution off the main thread.
    /// </summary>
    /// <param name="host">IP literal (e.g. "192.168.1.20") or hostname (e.g. "play.example.com").</param>
    /// <param name="port">UDP port the host is listening on.</param>
    /// <returns>True if the handshake succeeded, false otherwise.</returns>
    internal async Task<bool> JoinServerDirect(string host, int port)
    {
        ReloadedServerData data;
        try
        {
            data = await Task.Run(() => ReloadedServerData.ForDirectConnection(host, port));
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(typeof(ReloadedServer), $"Invalid direct connection target '{host}:{port}': {ex.Message}");
            return false;
        }

        lock (_stateLock)
        {
            Mode = ConnectionMode.DirectClient;
        }

        return await JoinServer(data);
    }

    /// <summary>
    /// Disconnects from the current lobby and cleans up resources.
    /// </summary>
    internal static void Leave()
    {
        if (Server == null)
            return;

        bool shouldRun;
        lock (Server._stateLock)
        {
            shouldRun = Server._isRunning;
            Server._isRunning = false;
        }

        if (!shouldRun)
            return;

        Server.ServerBroadcast?.StopBroadcasting();
        Server.P2PCTS?.Cancel();

        if (Server.IsHost)
        {
            var writer = PacketWriter.Get();

            writer.StartSubpacket((byte)ServerPacket.HandshakeLeave);
            writer.EndSubpacket();

            writer.StartSubpacket((byte)ServerPacket.SyncMembers);
            ReloadedServerProtocol.SerializeSyncMembers(writer, [], false);
            writer.EndSubpacket();

            foreach (var member in Server.GetOtherMembers())
            {
                Server.SendRawPacketTo(member.MemberId.AsIPEndPoint(), writer);
            }
            writer.Recycle();
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
                writer.StartSubpacket((byte)ServerPacket.HandshakeLeave);
                writer.EndSubpacket();
                Server.SendRawPacketTo(hostId.AsIPEndPoint(), writer);
                writer.Recycle();
            }
        }

        Server.P2PClient?.Close();
        Server.ServerData.Reset();

        lock (Server._membersLock) Server.Members.Clear();
        lock (Server._stateLock)
        {
            Server.PendingRequests.Clear();
            Server.IsConnected = false;
            Server.Mode = ConnectionMode.None;
        }
    }

    private async Task ListenForP2P()
    {
        while (!P2PCTS.Token.IsCancellationRequested)
        {
            try
            {
                var result = await P2PClient!.ReceiveAsync();
                ProcessPacketData(result.Buffer, result.RemoteEndPoint);
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
                ReplantedOnlineMod.Logger.Error(typeof(ReloadedServer), $"P2P listen error: {ex}");
                await Task.Delay(100, P2PCTS.Token);
            }
        }
    }

    internal ReloadedMemberData CreateMember(ID memberId, IPEndPoint iPEndPoint, string playerName)
    {
        var member = new ReloadedMemberData
        {
            PlayerName = playerName,
            MemberId = memberId
        };

        lock (_membersLock) Members[memberId] = member;
        return member;
    }

    internal void RemoveMember(ReloadedMemberData member)
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

            var writer = PacketWriter.Get();

            writer.StartSubpacket((byte)ServerPacket.HandshakeLeave);
            writer.EndSubpacket();

            writer.StartSubpacket((byte)ServerPacket.SyncMembers);
            lock (_membersLock) ReloadedServerProtocol.SerializeSyncMembers(writer, Members, false);
            writer.EndSubpacket();

            foreach (var otherMember in GetOtherMembers())
            {
                SendRawPacketTo(otherMember.MemberId.AsIPEndPoint(), writer);
            }
            writer.Recycle();
        }
    }

    internal void SendServerPacket(PacketWriter packetWriter)
    {
        foreach (var member in GetOtherMembers())
        {
            SendRawPacketTo(member.MemberId.AsIPEndPoint(), packetWriter);
        }
    }

    internal void SendRawPacketTo(IPEndPoint iPEndPoint, PacketWriter packetWriter)
    {
        if (iPEndPoint == null)
            return;

        var buffer = packetWriter.GetBytes();

        try
        {
            P2PClient!.Send(buffer, buffer.Length, iPEndPoint);
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(typeof(ReloadedServer), $"Failed to send packet to {iPEndPoint}: {ex}");
        }
    }

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
                ReplantedOnlineMod.Logger.Warning(typeof(ReloadedServer), $"Cannot send RPC to {memberId} - member not found");
                return false;
            }
        }

        var endpoint = memberId.AsIPEndPoint();
        if (endpoint == null) return false;

        var writer = PacketWriter.Get();
        writer.StartSubpacket((byte)ServerPacket.Rpc);
        ReloadedServerProtocol.SerializeRPC(writer, channel, data);
        writer.EndSubpacket();
        SendRawPacketTo(endpoint, writer);
        writer.Recycle();
        return true;
    }

    internal void SetLobbyData(string key, string value, bool remove = false)
    {
        if (!IsHost)
            return;

        if (remove) ServerData.Data.Remove(key);
        else ServerData.Data[key] = value;

        MainThreadDispatcher.Execute(() =>
        {
            if (Server != null && Server._isRunning)
                OnLobbyDataChanged?.Invoke(ServerData.ToServerLobby());
        });

        var writer = PacketWriter.Get();
        writer.StartSubpacket((byte)ServerPacket.LobbyDataUpdate);
        ReloadedServerProtocol.SerializeSetLobbyData(writer, key, value, remove);
        writer.EndSubpacket();
        SendServerPacket(writer);
        writer.Recycle();
    }

    internal void SetMemberData(string key, string value)
    {
        lock (_membersLock)
        {
            if (Members.TryGetValue(LocalMemberId, out var member))
                member.Data[key] = value;
        }

        var writer = PacketWriter.Get();
        writer.StartSubpacket((byte)ServerPacket.MemberData);
        ReloadedServerProtocol.SerializeMemberData(writer, key, value);
        writer.EndSubpacket();
        SendServerPacket(writer);
        writer.Recycle();
    }

    internal void ProcessPacketData(byte[] buffer, IPEndPoint remoteEndPoint)
    {
        var packetReader = PacketReader.Get(buffer);
        var senderId = new ID(remoteEndPoint, IdType.IPEndPoint);

        try
        {
            while (packetReader.Remaining > 0)
            {
                var subReader = packetReader.NextSubpacket();
                try
                {
                    var serverPacket = (ServerPacket)subReader.SubTag;
                    ProcessServerPacket(senderId, remoteEndPoint, serverPacket, subReader);
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

    private void ProcessServerPacket(ID senderId, IPEndPoint remoteEndPoint, ServerPacket serverPacket, PacketReader reader)
    {
        switch (serverPacket)
        {
            case ServerPacket.HandshakeRequest: ProcessHandshakeRequest(senderId, remoteEndPoint, reader); break;
            case ServerPacket.HandshakeAccept: ProcessHandshakeAccept(senderId, reader); break;
            case ServerPacket.HandshakeReject: ProcessHandshakeReject(senderId, reader); break;
            case ServerPacket.HandshakeLeave: ProcessHandshakeLeave(senderId); break;
            case ServerPacket.SyncMembers: ProcessSyncMembers(senderId, reader); break;
            case ServerPacket.Rpc: ProcessRPC(senderId, reader); break;
            case ServerPacket.LobbyData: ProcessLobbyData(senderId, reader); break;
            case ServerPacket.LobbyDataUpdate: ProcessLobbyDataUpdate(senderId, reader); break;
            case ServerPacket.MemberData: ProcessMemberData(senderId, reader); break;
        }
    }

    private void ProcessHandshakeRequest(ID senderId, IPEndPoint endpoint, PacketReader reader)
    {
        if (!IsHost)
            return;

        var (memberName, memberId) = ReloadedServerProtocol.DeserializeHandshakeRequest(reader);
        ValidateMemberName(ref memberName);

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

        var responseWriter = PacketWriter.Get();

        responseWriter.StartSubpacket((byte)ServerPacket.LobbyData);
        ReloadedServerProtocol.SerializeLobbyData(responseWriter, ServerData);
        responseWriter.EndSubpacket();

        responseWriter.StartSubpacket((byte)ServerPacket.SyncMembers);
        lock (_membersLock) ReloadedServerProtocol.SerializeSyncMembers(responseWriter, Members, true);
        responseWriter.EndSubpacket();

        responseWriter.StartSubpacket((byte)ServerPacket.HandshakeAccept);
        ReloadedServerProtocol.SerializeHandshakeAccept(responseWriter, ServerData.LobbyId);
        responseWriter.EndSubpacket();

        SendRawPacketTo(endpoint, responseWriter);
        responseWriter.Recycle();

        BroadcastSyncMembers(memberId);
    }

    private void ValidateMemberName(ref string memberName)
    {
        var sb = new System.Text.StringBuilder(memberName.Length);
        foreach (char c in memberName)
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == ' ')
                sb.Append(c);
        }

        string clean = System.Text.RegularExpressions.Regex
            .Replace(sb.ToString(), @"\s+", " ")
            .Trim();

        if (clean.Length == 0)
            clean = "Client";

        string candidate = clean;
        int suffix = 1;

        while (NameExists(candidate))
        {
            candidate = $"{clean} {suffix}";
            suffix++;
        }

        memberName = candidate;
    }

    private bool NameExists(string name)
    {
        lock (_membersLock)
        {
            foreach (var member in Members.Values)
            {
                if (string.Equals(member.PlayerName, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

    private void ProcessHandshakeAccept(ID senderId, PacketReader reader)
    {
        if (IsHost)
            return;

        var lobbyId = ReloadedServerProtocol.DeserializeHandshakeAccept(reader);
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

    private void ProcessHandshakeReject(ID senderId, PacketReader reader)
    {
        var reason = ReloadedServerProtocol.DeserializeHandshakeReject(reader);
        lock (_stateLock)
        {
            RejectionReasons[senderId] = reason;
            HandshakeCompletionSource?.TrySetResult(false);
        }
    }

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

    private void ProcessSyncMembers(ID senderId, PacketReader reader)
    {
        if (IsHost)
            return;

        var (isHandshake, syncedMembers) = ReloadedServerProtocol.DeserializeSyncMembers(reader);

        Dictionary<ID, ReloadedMemberData> oldMembers;
        lock (_membersLock) oldMembers = new Dictionary<ID, ReloadedMemberData>(Members);

        var newMembers = syncedMembers.Where(c => !oldMembers.ContainsKey(c.Key)).Select(c => c.Value).ToList();
        var removedMembers = oldMembers.Where(c => !syncedMembers.ContainsKey(c.Key)).Select(c => c.Value).ToList();

        lock (_membersLock)
        {
            Members.Clear();
            foreach (var member in syncedMembers) Members[member.Key] = member.Value;
        }

        ServerData.SetPlayerCount(Members.Count);

        if (!isHandshake)
        {
            MainThreadDispatcher.Execute(() =>
            {
                if (Server == null || !Server._isRunning)
                    return;

                var lobbyData = ServerData.ToServerLobby();
                foreach (var newMember in newMembers)
                {
                    OnLobbyMemberJoined?.Invoke(lobbyData, newMember.MemberId);
                }
                foreach (var removedMember in removedMembers)
                {
                    OnLobbyMemberLeave?.Invoke(lobbyData, removedMember.MemberId);
                }
            });
        }
    }

    private void ProcessRPC(ID senderId, PacketReader reader)
    {
        var (channel, data) = ReloadedServerProtocol.DeserializeRPC(reader);
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

    private void ProcessLobbyData(ID senderId, PacketReader reader)
    {
        if (!IsHost) ReloadedServerProtocol.DeserializeLobbyData(reader, ServerData);
    }

    private void ProcessLobbyDataUpdate(ID senderId, PacketReader reader)
    {
        if (IsHost)
            return;

        var (key, value, remove) = ReloadedServerProtocol.DeserializeSetLobbyData(reader);
        if (remove) ServerData.Data.Remove(key);
        else ServerData.Data[key] = value;

        MainThreadDispatcher.Execute(() =>
        {
            if (Server != null && Server._isRunning)
                OnLobbyDataChanged?.Invoke(ServerData.ToServerLobby());
        });
    }

    private void ProcessMemberData(ID senderId, PacketReader reader)
    {
        var (key, value) = ReloadedServerProtocol.DeserializeMemberData(reader);
        lock (_membersLock)
        {
            if (Members.TryGetValue(senderId, out var member))
                member.Data[key] = value;
        }
    }

    internal bool ReadP2PPacket(PacketBuffer buffer, PacketChannel channel)
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

    private void SendRejection(IPEndPoint endpoint, string reason)
    {
        var writer = PacketWriter.Get();
        writer.StartSubpacket((byte)ServerPacket.HandshakeReject);
        ReloadedServerProtocol.SerializeHandshakeReject(writer, reason);
        writer.EndSubpacket();
        SendRawPacketTo(endpoint, writer);
        writer.Recycle();
    }

    internal void BroadcastSyncMembers(ID ignoreMember)
    {
        var writer = PacketWriter.Get();
        writer.StartSubpacket((byte)ServerPacket.SyncMembers);
        lock (_membersLock) ReloadedServerProtocol.SerializeSyncMembers(writer, Members, false);
        writer.EndSubpacket();

        foreach (var member in GetOtherMembers())
        {
            if (member.MemberId == ignoreMember)
                continue;

            SendRawPacketTo(member.MemberId.AsIPEndPoint(), writer);
        }
        writer.Recycle();
    }

    private List<ReloadedMemberData> GetOtherMembers()
    {
        lock (_membersLock)
        {
            return Members.Values.Where(c => c.MemberId != LocalMemberId).ToList();
        }
    }

    /// <summary>
    /// Gets the local network IP address.
    /// </summary>
    internal static IPAddress GetLocalNetworkIP()
    {
        try
        {
            return Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork
                                   && !IPAddress.IsLoopback(ip)) ?? LOCALHOST_IP;
        }
        catch
        {
            return LOCALHOST_IP;
        }
    }

    /// <summary>
    /// Gets the port the server is listening on.
    /// </summary>
    internal static int GetPort()
    {
        if (Server == null || !Server._isRunning || !Server.IsHost) return 0;
        return Server.ServerData.GamePort;
    }

    public void Dispose()
    {
        ServerData?.Dispose();
        ServerBroadcast?.Dispose();
        P2PCTS?.Dispose();
        P2PClient?.Dispose();
    }

    internal sealed class PendingPacket : IDisposable
    {
        public byte[]? Data { get; set; }
        public ID SenderId { get; set; }
        public uint Size { get; set; }

        public void Dispose()
        {
            Data = null;
        }
    }
}
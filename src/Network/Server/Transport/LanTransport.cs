using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSteamworks;
using ReplantedOnline.Enums;
using ReplantedOnline.Enums.LAN;
using ReplantedOnline.Interfaces;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Monos;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Server.LAN;
using ReplantedOnline.Network.Server.Packet;
using ReplantedOnline.Structs;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ReplantedOnline.Network.Server.Transport;

/// <summary>
/// LAN-based network transport implementation using UDP for P2P communication.
/// Provides lobby discovery, connection management, and packet transmission similar to Steam P2P but over local network.
/// </summary>
internal sealed class LanTransport : INetworkTransport
{
    internal string PlayerName => IsHost ? "Client 1" : "Client 2";

    private const int BROADCAST_PORT = 14242;
    private const int GAME_PORT_BASE = 14243;
    private const int BROADCAST_INTERVAL_MS = 2000;
    private const int HANDSHAKE_TIMEOUT_MS = 5000;

    internal readonly UdpClient BroadcastListener;
    internal readonly UdpClient P2PListener;
    internal readonly CancellationTokenSource CTS = new();
    internal readonly object _lock = new();
    internal readonly Dictionary<PacketChannel, Queue<PendingPacket>> PacketQueue = [];

    private readonly ID _localClientId;
    internal readonly int GamePort;
    internal readonly int InstanceId;
    private readonly string _localIPAddress;

    internal ID CurrentLobbyId = ID.Null;
    internal bool IsHost;
    internal LanLobbyData CurrentLobbyData = Structs.LanLobbyData.Null;
    internal bool IsJoining = false;

    // Client storage
    internal readonly Dictionary<ID, ClientInfo> Clients = [];
    internal readonly Dictionary<ID, LanServerPresence> DiscoveredLobbies = [];

    // Lobby data storage
    internal readonly Dictionary<ID, Dictionary<string, string>> LobbyData = [];
    internal readonly Dictionary<ID, Dictionary<ID, Dictionary<string, string>>> MemberData = [];

    // Connection state tracking
    internal enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Handshaking
    }
    internal readonly Dictionary<ID, ConnectionState> ConnectionStates = [];

    // Pending handshakes
    internal TaskCompletionSource<bool> HandshakeCompletionSource;
    internal readonly HashSet<ID> PendingRequests = [];

    public ID LocalClientId => _localClientId;

    public LanTransport()
    {
        InstanceId = GenerateInstanceId();
        _localClientId = ID.CreateRandomULong();
        GamePort = CalculateGamePort();
        _localIPAddress = GetLocalIPAddress();

        ReplantedOnlineMod.Logger.Msg($"[LAN] Instance {InstanceId} | Local ID: {_localClientId} | Game Port: {GamePort} | IP: {_localIPAddress}");

        BroadcastListener = CreateBroadcastListener();
        P2PListener = CreateP2PListener();

        foreach (var channel in Enum.GetValues<PacketChannel>())
        {
            PacketQueue[channel] = [];
        }

        Task.Run(ListenForBroadcasts, CTS.Token);
        Task.Run(ListenForP2P, CTS.Token);
    }

    private static string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch { }
        return "127.0.0.1";
    }

    private static int GenerateInstanceId()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        return Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1000;
    }

    private int CalculateGamePort() => GAME_PORT_BASE + (InstanceId % 100);

    private static UdpClient CreateBroadcastListener()
    {
        try
        {
            var client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(new IPEndPoint(IPAddress.Any, BROADCAST_PORT));
            client.EnableBroadcast = true;
            return client;
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error($"[LAN] Failed to bind broadcast listener: {ex.Message}");
            throw;
        }
    }

    private UdpClient CreateP2PListener()
    {
        try
        {
            var client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            try
            {
                client.Client.Bind(new IPEndPoint(IPAddress.Any, GamePort));
            }
            catch (SocketException)
            {
                client.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
                var actualPort = ((IPEndPoint)client.Client.LocalEndPoint).Port;
                ReplantedOnlineMod.Logger.Msg($"[LAN] Using random port {actualPort}");
            }

            return client;
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error($"[LAN] Failed to bind P2P listener: {ex.Message}");
            throw;
        }
    }

    public void Tick(float deltaTime) { }

    // ===== PUBLIC API =====

    public async Task JoinFirstLanLobby()
    {
        if (IsJoining) return;

        try
        {
            IsJoining = true;
            HandshakeCompletionSource = new TaskCompletionSource<bool>();
            ReplantedOnlineMod.Logger.Msg("[LAN] Searching for lobbies...");

            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalSeconds < 3)
            {
                if (DiscoveredLobbies.Count > 0)
                {
                    var lobby = DiscoveredLobbies.Values.First();
                    ReplantedOnlineMod.Logger.Msg($"[LAN] Found lobby: {lobby.ServerName} at {lobby.EndPoint}:{lobby.GamePort}");

                    JoinLobby(lobby.LobbyId);

                    // Wait for handshake to complete with timeout
                    var completedTask = await Task.WhenAny(
                        HandshakeCompletionSource.Task,
                        Task.Delay(HANDSHAKE_TIMEOUT_MS)
                    );

                    if (completedTask == HandshakeCompletionSource.Task &&
                        await HandshakeCompletionSource.Task)
                    {
                        if (CurrentLobbyId == lobby.LobbyId)
                        {
                            ReplantedOnlineMod.Logger.Msg($"[LAN] Successfully joined lobby {lobby.ServerName}");
                            MainThreadDispatcher.Execute(() =>
                            {
                                NetLobby.OnLobbyEnteredCompleted(CurrentLobbyData);
                            });
                            IsJoining = false;
                            return;
                        }
                    }

                    ReplantedOnlineMod.Logger.Error($"[LAN] Failed to join lobby - handshake timeout");
                    CurrentLobbyId = ID.Null;
                    CurrentLobbyData = Structs.LanLobbyData.Null;
                    IsJoining = false;
                    ShowDisconnectPopup("Failed to join LAN lobby - timeout");
                    return;
                }
                await Task.Delay(100, CTS.Token);
            }

            ReplantedOnlineMod.Logger.Msg("[LAN] No lobbies found");
            IsJoining = false;
            ShowDisconnectPopup("No LAN lobbies found");
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error($"[LAN] Error: {ex.Message}");
            IsJoining = false;
            ShowDisconnectPopup("Error joining LAN lobby");
        }
    }

    public void CreateLobby(int maxPlayers)
    {
        if (!CurrentLobbyId.IsNull) return;

        lock (_lock)
        {
            var lobbyId = ID.CreateRandomULong();

            string lobbyName = $"{PlayerName}'s Lobby";

            CurrentLobbyData = new LanLobbyData(
                lobbyId: lobbyId,
                ownerId: _localClientId,
                isJoinable: true,
                maxPlayers: maxPlayers,
                modVersion: ModInfo.MOD_VERSION_FORMATTED,
                gameCode: MatchmakingManager.GenerateGameCode(lobbyId),
                name: lobbyName
            );

            CurrentLobbyId = lobbyId;
            IsHost = true;

            // Initialize storage
            LobbyData[lobbyId] = [];
            MemberData[lobbyId] = [];

            // Add self with player name
            var localEndPoint = (IPEndPoint)P2PListener.Client.LocalEndPoint;
            AddClient(_localClientId, localEndPoint, PlayerName);
            MemberData[lobbyId][_localClientId] = [];
            ConnectionStates[_localClientId] = ConnectionState.Connected;

            ReplantedOnlineMod.Logger.Msg($"[LAN] Hosting lobby: {lobbyName} | Host: {PlayerName} | Code: {CurrentLobbyData.GameCode} | Port: {GamePort}");

            MainThreadDispatcher.Execute(() =>
            {
                NetLobby.OnLobbyCreatedCompleted(Result.OK, CurrentLobbyData);
                NetLobby.OnLobbyEnteredCompleted(CurrentLobbyData);
            });

            Task.Run(BroadcastPresence, CTS.Token);
        }
    }

    public void JoinLobby(ID lobbyId)
    {
        lock (_lock)
        {
            if (!CurrentLobbyId.IsNull) return;

            if (!DiscoveredLobbies.TryGetValue(lobbyId, out var lobby))
            {
                ReplantedOnlineMod.Logger.Error($"[LAN] Lobby {lobbyId} not found");
                return;
            }

            ReplantedOnlineMod.Logger.Msg($"[LAN] Joining lobby {lobbyId}");

            CurrentLobbyId = lobbyId;
            IsHost = false;
            CurrentLobbyData = LanLobbyData.CreateLobbyDataFromPresence(lobby);

            // Add YOURSELF as a client with player name
            var localEndPoint = (IPEndPoint)P2PListener.Client.LocalEndPoint;
            AddClient(_localClientId, localEndPoint, PlayerName);
            ConnectionStates[_localClientId] = ConnectionState.Connected;

            // Initialize member data
            if (!MemberData.ContainsKey(lobbyId))
                MemberData[lobbyId] = [];
            if (!MemberData[lobbyId].ContainsKey(_localClientId))
                MemberData[lobbyId][_localClientId] = [];

            // Store host info with correct endpoint (temporary name until we get real one)
            var hostEndPoint = new IPEndPoint(IPAddress.Parse(lobby.EndPoint), lobby.GamePort);
            AddClient(lobby.ServerId, hostEndPoint, lobby.ServerName);
            ConnectionStates[lobby.ServerId] = ConnectionState.Handshaking;

            ReplantedOnlineMod.Logger.Msg($"[LAN] Joining as {PlayerName}");

            // Send handshake request WITH our client info
            var clientInfo = new ClientInfo
            {
                ClientId = _localClientId,
                Name = PlayerName,
                EndPoint = localEndPoint
            };
            LanDispatcher.SendHandshake(lobby.ServerId, LanHandshakeType.Request, clientInfo);
        }
    }

    public void LeaveLobby(ID lobbyId)
    {
        lock (_lock)
        {
            if (CurrentLobbyId != lobbyId) return;

            ReplantedOnlineMod.Logger.Msg($"[LAN] Leaving lobby {lobbyId}");

            if (!IsHost && DiscoveredLobbies.TryGetValue(lobbyId, out var lobby))
            {
                LanDispatcher.SendHandshake(lobby.ServerId, LanHandshakeType.Leave);
            }

            CleanupLobby();
        }
    }

    public void StopHosting()
    {
        lock (_lock)
        {
            if (!IsHost || !CurrentLobbyId.HasValue) return;

            ReplantedOnlineMod.Logger.Msg($"[LAN] Stopping host");

            foreach (var client in Clients.Values.Where(c => c.ClientId != _localClientId && c.EndPoint != null))
            {
                LanDispatcher.SendHandshake(client.ClientId, LanHandshakeType.Leave);
            }

            CleanupLobby();
        }
    }

    private void CleanupLobby()
    {
        CurrentLobbyId = ID.Null;
        CurrentLobbyData = LanLobbyData.Null;
        IsHost = false;
        IsJoining = false;
        PendingRequests.Clear();
        ConnectionStates.Clear();
        foreach (var channel in Enum.GetValues<PacketChannel>())
        {
            PacketQueue[channel].Clear();
        }
        var keepClients = Clients.Keys.Where(DiscoveredLobbies.ContainsKey).ToList();
        var toRemove = Clients.Keys.Except(keepClients).ToList();
        foreach (var id in toRemove)
        {
            Clients.Remove(id);
        }
    }

    private void BroadcastInternalPacket(LanPacketType type, Action<PacketWriter> writeContent, bool excludeSelf, ID excludeClient)
    {
        foreach (var client in Clients.Values)
        {
            if (excludeSelf && client.ClientId == _localClientId) continue;
            if (!excludeClient.IsNull && client.ClientId == excludeClient) continue;
            if (client.EndPoint == null) continue;
            if (ConnectionStates.TryGetValue(client.ClientId, out var state) && state != ConnectionState.Connected) continue;

            try
            {
                LanDispatcher.SendInternalPacket(client.ClientId, type, writeContent);
            }
            catch { }
        }
    }

    // ===== EXTERNAL RPC PACKET METHODS =====

    public bool SendP2PPacket(ID clientId, Il2CppStructArray<byte> data, int length = -1, PacketChannel channel = PacketChannel.Main, P2PSend sendType = P2PSend.Reliable)
    {
        if (clientId == _localClientId)
        {
            QueueLocalPacket(data, length, channel);
            return true;
        }

        if (!ConnectionStates.TryGetValue(clientId, out var state) || state != ConnectionState.Connected)
            return false;

        var endpoint = GetClientEndpoint(clientId);
        if (endpoint == null) return false;

        var writer = PacketWriter.Get();
        try
        {
            writer.AddTag(PacketTag.Rpc);
            writer.WriteID(_localClientId);
            writer.WriteInt((int)channel);
            writer.WriteBytes(data);
            var packetData = writer.GetBytes();

            P2PListener.Send(packetData, packetData.Length, endpoint);
            return true;
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error($"[LAN] Send error: {ex.Message}");
            return false;
        }
        finally
        {
            writer.Recycle();
        }
    }

    public bool ReadP2PPacket(P2PPacketBuffer buffer, PacketChannel channel = PacketChannel.Main)
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

    public bool IsP2PPacketAvailable(out uint msgSize, PacketChannel channel = PacketChannel.Main)
    {
        lock (PacketQueue)
        {
            if (PacketQueue.TryGetValue(channel, out var queue) && queue.Count > 0)
            {
                msgSize = queue.Peek().Size;
                return true;
            }
        }
        msgSize = 0;
        return false;
    }

    // ===== LISTENERS =====

    private async Task BroadcastPresence()
    {
        var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, BROADCAST_PORT);

        while (!CTS.Token.IsCancellationRequested && IsHost)
        {
            try
            {
                var presence = new LanServerPresence
                {
                    ServerId = _localClientId,
                    ServerName = CurrentLobbyData.Name,
                    PlayerCount = GetNumLobbyMembers(CurrentLobbyId),
                    MaxPlayers = CurrentLobbyData.MaxPlayers,
                    LobbyId = CurrentLobbyId,
                    IsJoinable = true,
                    ModVersion = CurrentLobbyData.ModVersion,
                    GameCode = CurrentLobbyData.GameCode,
                    GamePort = GamePort,
                    EndPoint = _localIPAddress
                };

                var data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(presence));
                await BroadcastListener.SendAsync(data, data.Length, broadcastEndpoint);
                await Task.Delay(BROADCAST_INTERVAL_MS, CTS.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                ReplantedOnlineMod.Logger.Error($"[LAN] Broadcast error: {ex.Message}");
                await Task.Delay(1000, CTS.Token);
            }
        }
    }

    private async Task ListenForBroadcasts()
    {
        while (!CTS.Token.IsCancellationRequested)
        {
            try
            {
                var result = await BroadcastListener.ReceiveAsync();

                if (result.Buffer.Length == 0 || result.Buffer[0] != '{')
                    continue;

                var json = Encoding.UTF8.GetString(result.Buffer);
                var presence = JsonSerializer.Deserialize<LanServerPresence>(json);

                if (presence != null && presence.ServerId != _localClientId)
                {
                    // Verify mod version compatibility
                    if (presence.ModVersion != ModInfo.MOD_VERSION_FORMATTED)
                    {
                        ReplantedOnlineMod.Logger.Warning($"[LAN] Ignoring lobby with mismatched mod version: {presence.ModVersion} vs {ModInfo.MOD_VERSION_FORMATTED}");
                        continue;
                    }

                    lock (_lock)
                    {
                        DiscoveredLobbies[presence.LobbyId] = presence;
                        // Don't automatically add as connected client - just discovered
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (ex.Message.StartsWith("The I/O operation has been aborted")) return;

                ReplantedOnlineMod.Logger.Error($"[LAN] Broadcast listen error: {ex.Message}");
                await Task.Delay(1000, CTS.Token);
            }
        }
    }

    private async Task ListenForP2P()
    {
        while (!CTS.Token.IsCancellationRequested)
        {
            try
            {
                var result = await P2PListener.ReceiveAsync();
                ProcessPacket(result.Buffer, result.RemoteEndPoint);
            }
            catch (Exception ex)
            {
                if (ex.Message.StartsWith("The I/O operation has been aborted")) return;

                ReplantedOnlineMod.Logger.Error($"[LAN] P2P listen error: {ex.Message}");
                await Task.Delay(100, CTS.Token);
            }
        }
    }

    private void ProcessPacket(byte[] buffer, IPEndPoint endpoint)
    {
        var reader = PacketReader.Get(buffer);
        try
        {
            var tag = reader.GetTag();
            var senderId = reader.ReadID();

            if (senderId == _localClientId) return;

            UpdateClientEndpoint(senderId, endpoint);

            switch (tag)
            {
                case PacketTag.LAN:
                    LanDispatcher.HandleLanPacket(senderId, reader, this);
                    break;

                case PacketTag.Rpc:
                    if (ConnectionStates.TryGetValue(senderId, out var state) && state == ConnectionState.Connected)
                    {
                        LanDispatcher.HandleRPCPacket(senderId, reader, this);
                    }
                    else
                    {
                        ReplantedOnlineMod.Logger.Warning($"[LAN] Ignoring RPC from unconnected client {senderId}");
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error($"[LAN] Process error: {ex.Message}");
        }
        finally
        {
            reader.Recycle();
        }
    }

    // ===== SESSION MANAGEMENT =====

    public bool AcceptP2PSessionWithUser(ID clientId)
    {
        lock (_lock)
        {
            if (!PendingRequests.Contains(clientId))
            {
                ReplantedOnlineMod.Logger.Warning($"[LAN] No pending request from {clientId}");
                return false;
            }

            ReplantedOnlineMod.Logger.Msg($"[LAN] Accepting connection from {clientId}");

            ConnectionStates[clientId] = ConnectionState.Connected;
            LanDispatcher.SendHandshake(clientId, LanHandshakeType.Accept);
            PendingRequests.Remove(clientId);

            if (IsHost && CurrentLobbyId.HasValue)
            {
                LanDispatcher.SendClientInfo(clientId);
                LanDispatcher.SendExistingDataToClient(clientId, this);
            }

            return true;
        }
    }

    public bool CloseP2PSessionWithUser(ID clientId)
    {
        lock (_lock)
        {
            if (ConnectionStates.TryGetValue(clientId, out var state) && state == ConnectionState.Connected)
            {
                LanDispatcher.SendHandshake(clientId, LanHandshakeType.Leave);
                RemoveClient(clientId);
            }
            return true;
        }
    }

    // ===== LOBBY DATA =====

    public string GetLobbyData(ID lobbyId, string key)
    {
        lock (_lock)
        {
            return LobbyData.TryGetValue(lobbyId, out var data) &&
                   data.TryGetValue(key, out var value) ? value : string.Empty;
        }
    }

    public bool SetLobbyData(ID lobbyId, string key, string value)
    {
        lock (_lock)
        {
            if (!IsHost || CurrentLobbyId != lobbyId) return false;

            if (!LobbyData.ContainsKey(lobbyId))
                LobbyData[lobbyId] = [];

            LobbyData[lobbyId][key] = value;

            bool skip = UpdateLobbyDataStruct(key, value);

            BroadcastInternalPacket(LanPacketType.LobbyData,
                writer =>
                {
                    writer.WriteString(key);
                    writer.WriteString(value);
                },
                true, ID.Null);

            if (!skip)
                MainThreadDispatcher.Execute(() =>
                {
                    NetLobby.OnLobbyDataChanged(CurrentLobbyData);
                });

            return true;
        }
    }

    public bool DeleteLobbyData(ID lobbyId, string key) => false;
    public bool RequestLobbyData(ID lobbyId) => true;

    // ===== MEMBER DATA =====

    public string GetLobbyMemberData(ID lobbyId, ID clientId, string key)
    {
        lock (_lock)
        {
            return MemberData.TryGetValue(lobbyId, out var lobby) &&
                   lobby.TryGetValue(clientId, out var data) &&
                   data.TryGetValue(key, out var value) ? value : string.Empty;
        }
    }

    public void SetLobbyMemberData(ID lobbyId, string key, string value)
    {
        lock (_lock)
        {
            if (CurrentLobbyId != lobbyId) return;

            if (!MemberData.ContainsKey(lobbyId))
                MemberData[lobbyId] = [];
            if (!MemberData[lobbyId].ContainsKey(_localClientId))
                MemberData[lobbyId][_localClientId] = [];

            MemberData[lobbyId][_localClientId][key] = value;

            if (IsHost)
            {
                BroadcastInternalPacket(LanPacketType.MemberData,
                    writer =>
                    {
                        writer.WriteID(_localClientId);
                        writer.WriteString(key);
                        writer.WriteString(value);
                    },
                    true, ID.Null);
            }
            else if (DiscoveredLobbies.TryGetValue(lobbyId, out var lobby))
            {
                LanDispatcher.SendInternalPacket(lobby.ServerId, LanPacketType.MemberData,
                    writer =>
                    {
                        writer.WriteID(_localClientId);
                        writer.WriteString(key);
                        writer.WriteString(value);
                    });
            }
        }
    }

    // ===== MEMBER MANAGEMENT =====

    public int GetNumLobbyMembers(ID lobbyId)
    {
        lock (_lock)
        {
            return MemberData.TryGetValue(lobbyId, out var data) ? data.Count : 0;
        }
    }

    public ID GetLobbyMemberByIndex(ID lobbyId, int index)
    {
        lock (_lock)
        {
            if (!MemberData.TryGetValue(lobbyId, out var data))
                return ID.Null;

            var list = data.Keys.ToList();
            return index >= 0 && index < list.Count ? list[index] : ID.Null;
        }
    }

    public string GetMemberName(ID clientId)
    {
        if (clientId == _localClientId)
            return PlayerName;

        lock (_lock)
        {
            return Clients.TryGetValue(clientId, out var client) ? client.Name : "Unknown";
        }
    }

    public bool SetLobbyMemberLimit(ID lobbyId, int maxMembers)
        => SetLobbyData(lobbyId, "max_players", maxMembers.ToString());

    public bool SetLobbyJoinable(ID lobbyId, bool joinable)
        => SetLobbyData(lobbyId, "joinable", joinable.ToString());

    public bool SetLobbyType(ID lobbyId, LobbyType type) => true;

    public ID GetLobbyOwner(ID lobbyId)
    {
        lock (_lock)
        {
            if (CurrentLobbyId == lobbyId)
            {
                return CurrentLobbyData.OwnerId;
            }

            return DiscoveredLobbies.TryGetValue(lobbyId, out var presence) ?
                   presence.ServerId : ID.Null;
        }
    }

    // ===== HELPERS =====

    internal void AddClient(ID clientId, IPEndPoint endpoint, string name)
    {
        if (Clients.TryGetValue(clientId, out var client))
        {
            client.Name = name;
            client.LastSeen = DateTime.UtcNow;
            if (endpoint != null) client.EndPoint = endpoint;
        }
        else
        {
            Clients[clientId] = new ClientInfo
            {
                ClientId = clientId,
                EndPoint = endpoint,
                Name = name,
                LastSeen = DateTime.UtcNow
            };
        }
    }

    private void UpdateClientEndpoint(ID clientId, IPEndPoint endpoint)
    {
        if (Clients.TryGetValue(clientId, out var client))
        {
            client.EndPoint = endpoint;
            client.LastSeen = DateTime.UtcNow;
        }
        else
        {
            Clients[clientId] = new ClientInfo
            {
                ClientId = clientId,
                EndPoint = endpoint,
                Name = $"Player_{clientId.AsULong():X8}",
                LastSeen = DateTime.UtcNow
            };
        }
    }

    internal IPEndPoint GetClientEndpoint(ID clientId)
    {
        return Clients.TryGetValue(clientId, out var client) ? client.EndPoint : null;
    }

    internal void RemoveClient(ID clientId)
    {
        Clients.Remove(clientId);
        ConnectionStates.Remove(clientId);
        if (CurrentLobbyId.HasValue && MemberData.TryGetValue(CurrentLobbyId, out var lobby))
        {
            lobby.Remove(clientId);
            MainThreadDispatcher.Execute(() =>
            {
                NetLobby.OnLobbyMemberLeave(CurrentLobbyData, clientId);
            });
        }
    }

    internal bool UpdateLobbyDataStruct(string key, string value)
    {
        switch (key)
        {
            case "max_players" when int.TryParse(value, out var max):
                CurrentLobbyData.MaxPlayers = max;
                return true;
            case "name":
                CurrentLobbyData.Name = value;
                return true;
            case "joinable" when bool.TryParse(value, out var joinable):
                CurrentLobbyData.IsJoinable = joinable;
                return true;
        }

        return false;
    }

    private void QueueLocalPacket(Il2CppStructArray<byte> data, int length, PacketChannel channel)
    {
        int actualLength = length == -1 ? data.Length : length;
        var localData = new byte[actualLength];
        Array.Copy(data, 0, localData, 0, actualLength);

        lock (PacketQueue)
        {
            PacketQueue[channel].Enqueue(new PendingPacket
            {
                Data = localData,
                SenderId = _localClientId,
                Size = (uint)localData.Length
            });
        }
    }

    private static void ShowDisconnectPopup(string message)
    {
        Transitions.ToMainMenu(() =>
        {
            ReplantedOnlinePopup.Show("Disconnected", message);
        });
    }

    public void Dispose()
    {
        try
        {
            CTS.Cancel();
            CTS.Dispose();
            BroadcastListener?.Close();
            P2PListener?.Close();
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error($"[LAN] Dispose error: {ex.Message}");
        }
    }
}
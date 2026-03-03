using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Enums;
using ReplantedOnline.Interfaces;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Server.Packet;
using ReplantedOnline.Structs;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ReplantedOnline.Network.Server.Transport;

internal sealed class LanTransport : INetworkTransport
{
    private readonly UdpClient _broadcastListener;
    private readonly UdpClient _p2pListener;
    private readonly Dictionary<ID, LanClientInfo> _clients = [];
    private readonly Dictionary<ID, Dictionary<string, string>> _lobbyData = []; // Host-managed data (synced to all)
    private readonly Dictionary<ID, Dictionary<ID, Dictionary<string, string>>> _clientLobbyData = []; // Per-client data (synced to all)
    private readonly Dictionary<ID, LanServerPresence> _discoveredLobbies = [];
    private readonly Queue<PendingPacket> _packetQueue = new();
    private readonly object _lock = new();
    private bool _isUpdatingLobbyData = false;
    private bool _isUpdatingClientData = false;

    private readonly ID _localClientId;
    private ID _currentLobbyId = ID.Null;
    private bool _isHost;
    private LobbyData _currentLobbyData = LobbyData.Null;
    private readonly int _broadcastPort = 14242;
    private readonly int _gamePort = 14243;
    private readonly CancellationTokenSource _cts;

    public event Action<LanServerPresence> OnLobbyDiscovered;

    public ID LocalClientId => _localClientId;

    public LanTransport()
    {
        // Generate random client ID
        ulong randomId;
        using (var rng = RandomNumberGenerator.Create())
        {
            var bytes = new byte[8];
            rng.GetBytes(bytes);
            randomId = BitConverter.ToUInt64(bytes, 0);
        }
        _localClientId = new ID(randomId, IdType.ULong);

        try
        {
            _broadcastListener = new UdpClient();
            _broadcastListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _broadcastListener.Client.Bind(new IPEndPoint(IPAddress.Any, _broadcastPort));
            _broadcastListener.EnableBroadcast = true;
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[LAN] Failed to bind broadcast listener: {ex.Message}");
            throw;
        }

        try
        {
            _p2pListener = new UdpClient();
            _p2pListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            // For multiple instances on same machine, we need to allow port sharing
            _p2pListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _p2pListener.Client.Bind(new IPEndPoint(IPAddress.Any, _gamePort));
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[LAN] Failed to bind P2P listener: {ex.Message}");
            throw;
        }

        _cts = new CancellationTokenSource();
        StartListening();
    }

    private void StartListening()
    {
        Task.Run(ListenForBroadcasts, _cts.Token);
        Task.Run(ListenForP2PPackets, _cts.Token);
    }

    public void Tick(float deltaTime)
    {
    }

    // ===== Public LAN Methods =====
    public async Task JoinFirstLanLobby()
    {
        try
        {
            MelonLogger.Msg("[LAN] Searching for first available lobby...");

            DateTime startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalSeconds < 3)
            {
                if (_discoveredLobbies.Count > 0)
                {
                    var firstLobby = _discoveredLobbies.Values.First();
                    MelonLogger.Msg($"[LAN] Found lobby: {firstLobby.ServerName} ({firstLobby.PlayerCount}/{firstLobby.MaxPlayers})");

                    JoinLobby(firstLobby.LobbyId);
                    await Task.Delay(500);

                    bool success = _currentLobbyId == firstLobby.LobbyId;
                    if (success)
                    {
                        MelonLogger.Msg($"[LAN] Successfully joined lobby: {firstLobby.LobbyId}");

                        // Create LobbyData from presence info
                        var lobbyData = new LobbyData(
                            lobbyId: firstLobby.LobbyId,
                            ownerId: firstLobby.ServerId,
                            isJoinable: firstLobby.IsJoinable,
                            maxPlayers: firstLobby.MaxPlayers,
                            modVersion: firstLobby.ModVersion,
                            gameCode: firstLobby.GameCode,
                            name: firstLobby.ServerName
                        );

                        _currentLobbyData = lobbyData;
                        NetLobby.OnLobbyEnteredCompleted(lobbyData);
                    }
                    else
                    {
                        MelonLogger.Error($"[LAN] Failed to join lobby: {firstLobby.LobbyId}");

                        Transitions.ToMainMenu(() =>
                        {
                            ReplantedOnlinePopup.Show("Disconnected", $"Unable to find LAN lobby!");
                        });
                    }

                    return;
                }
                await Task.Delay(100);
            }

            MelonLogger.Msg("[LAN] No lobbies found");

            Transitions.ToMainMenu(() =>
            {
                ReplantedOnlinePopup.Show("Disconnected", $"Unable to find LAN lobby!");
            });
            return;
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[LAN] Error finding lobby: {ex.Message}");

            Transitions.ToMainMenu(() =>
            {
                ReplantedOnlinePopup.Show("Disconnected", $"An error occurred joining LAN lobby!");
            });
            return;
        }
    }

    public void CreateLobby(int maxPlayers)
    {
        if (!_currentLobbyId.IsNull)
            return;

        try
        {
            lock (_lock)
            {
                // Generate random lobby ID
                ulong randomId;
                using (var rng = RandomNumberGenerator.Create())
                {
                    var bytes = new byte[8];
                    rng.GetBytes(bytes);
                    randomId = BitConverter.ToUInt64(bytes, 0);
                }

                ID lobbyId = new(randomId, IdType.ULong);

                _currentLobbyData = new LobbyData(
                    lobbyId: lobbyId,
                    ownerId: _localClientId,
                    isJoinable: true,
                    maxPlayers: maxPlayers,
                    modVersion: ModInfo.MOD_VERSION_FORMATTED,
                    gameCode: MatchmakingManager.GenerateGameCode(randomId),
                    name: Environment.MachineName
                );

                _currentLobbyId = lobbyId;
                _isHost = true;

                // Initialize lobby data storage
                _lobbyData[lobbyId] = [];
                _clientLobbyData[lobbyId] = [];

                _clients[_localClientId] = new LanClientInfo
                {
                    ClientId = _localClientId,
                    EndPoint = null,
                    Name = _currentLobbyData.Name,
                    LastSeen = DateTime.UtcNow
                };

                MelonLogger.Msg($"[LAN] Hosting lobby: {lobbyId}");
                MelonLogger.Msg($"[LAN] Max players: {maxPlayers}");
                MelonLogger.Msg($"[LAN] Game code: {_currentLobbyData.GameCode}");

                // Trigger callbacks
                NetLobby.OnLobbyCreatedCompleted(Result.OK, _currentLobbyData);
                NetLobby.OnLobbyEnteredCompleted(_currentLobbyData);

                // Start broadcasting presence
                Task.Run(BroadcastPresence, _cts.Token);
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[LAN] Failed to host lobby: {ex.Message}");
            NetLobby.OnLobbyCreatedCompleted(Result.Fail, LobbyData.Null);
        }
    }

    public void StopHosting()
    {
        lock (_lock)
        {
            if (_isHost && _currentLobbyId.HasValue)
            {
                MelonLogger.Msg($"[LAN] Stopping host for lobby: {_currentLobbyId}");

                // Notify all members
                foreach (var client in _clients.Values.Where(c => c.ClientId != _localClientId))
                {
                    CloseP2PSessionWithUser(client.ClientId);
                }

                _lobbyData.Remove(_currentLobbyId);
                _clientLobbyData.Remove(_currentLobbyId);
                _currentLobbyId = ID.Null;
                _currentLobbyData = LobbyData.Null;
                _isHost = false;
            }
        }
    }

    // ===== Broadcast Discovery Methods =====
    private async Task BroadcastPresence()
    {
        var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, _broadcastPort);

        while (!_cts.Token.IsCancellationRequested && _isHost)
        {
            try
            {
                var presence = new LanServerPresence
                {
                    ServerId = _localClientId,
                    ServerName = _currentLobbyData.Name,
                    PlayerCount = GetNumLobbyMembers(_currentLobbyId),
                    MaxPlayers = _currentLobbyData.MaxPlayers,
                    LobbyId = _currentLobbyId,
                    IsJoinable = _currentLobbyData.IsJoinable,
                    ModVersion = _currentLobbyData.ModVersion,
                    GameCode = _currentLobbyData.GameCode
                };

                var json = JsonSerializer.Serialize(presence);
                var data = Encoding.UTF8.GetBytes(json);

                await _broadcastListener.SendAsync(data, data.Length, broadcastEndpoint);
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                if (!_cts.Token.IsCancellationRequested)
                    MelonLogger.Error($"[LAN] Broadcast error: {ex.Message}");
            }
        }
    }

    private async Task ListenForBroadcasts()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var result = await _broadcastListener.ReceiveAsync();

                // Skip if not JSON (starts with '{')
                if (result.Buffer.Length == 0 || result.Buffer[0] != '{')
                    continue;

                var json = Encoding.UTF8.GetString(result.Buffer);
                var presence = JsonSerializer.Deserialize<LanServerPresence>(json);

                if (presence != null && presence.ServerId != _localClientId)
                {
                    lock (_lock)
                    {
                        _discoveredLobbies[presence.ServerId] = presence;

                        if (!_clients.ContainsKey(presence.ServerId))
                        {
                            _clients[presence.ServerId] = new LanClientInfo
                            {
                                ClientId = presence.ServerId,
                                EndPoint = result.RemoteEndPoint,
                                Name = presence.ServerName,
                                LastSeen = DateTime.UtcNow
                            };

                            MelonLogger.Msg($"[LAN] Discovered server: {presence.ServerName}");
                            OnLobbyDiscovered?.Invoke(presence);
                        }
                        else
                        {
                            _clients[presence.ServerId].LastSeen = DateTime.UtcNow;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_cts.Token.IsCancellationRequested)
                    MelonLogger.Error($"[LAN] Broadcast listen error: {ex.Message}");
            }
        }
    }

    // ===== P2P Packet Methods =====
    private async Task ListenForP2PPackets()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var result = await _p2pListener.ReceiveAsync();
                var reader = PacketReader.Get(result.Buffer);

                try
                {
                    var packetType = (LanPacketType)reader.ReadByte();
                    var senderId = reader.ReadID();

                    lock (_lock)
                    {
                        if (_clients.TryGetValue(senderId, out var client))
                            client.LastSeen = DateTime.UtcNow;
                    }

                    switch (packetType)
                    {
                        case LanPacketType.P2PData:
                            var data = reader.ReadBytes();
                            lock (_packetQueue)
                            {
                                _packetQueue.Enqueue(new PendingPacket
                                {
                                    Data = data,
                                    SenderId = senderId,
                                    Size = (uint)data.Length
                                });
                            }
                            break;

                        case LanPacketType.P2PSessionRequest:
                            HandleP2PSessionRequest(senderId, result.RemoteEndPoint);
                            break;

                        case LanPacketType.P2PSessionAccept:
                            HandleP2PSessionAccept(senderId);
                            break;

                        case LanPacketType.P2PSessionClose:
                            HandleP2PSessionClose(senderId);
                            break;

                        case LanPacketType.LobbyData:
                            HandleLobbyData(senderId, reader);
                            break;

                        case LanPacketType.ClientLobbyData:
                            HandleClientLobbyData(senderId, reader);
                            break;

                        case LanPacketType.ClientInfo:
                            HandleClientInfo(senderId, reader);
                            break;
                    }
                }
                finally
                {
                    reader.Recycle();
                }
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (!_cts.Token.IsCancellationRequested)
                    MelonLogger.Error($"[LAN] P2P listen error: {ex.Message}");
                await Task.Delay(100);
            }
        }
    }

    public bool IsP2PPacketAvailable(out uint msgSize, int channel = 0)
    {
        lock (_packetQueue)
        {
            if (_packetQueue.Count > 0)
            {
                msgSize = _packetQueue.Peek().Size;
                return true;
            }
        }
        msgSize = 0;
        return false;
    }

    public bool SendP2PPacket(ID clientId, Il2CppStructArray<byte> data, int length = -1, int nChannel = 0, P2PSend sendType = P2PSend.Reliable)
    {
        // Handle self-packets locally
        if (clientId == _localClientId)
        {
            byte[] dataToProcess = length >= 0 ? data.Take(length).ToArray() : data.ToArray();

            lock (_packetQueue)
            {
                _packetQueue.Enqueue(new PendingPacket
                {
                    Data = dataToProcess,
                    SenderId = _localClientId,
                    Size = (uint)dataToProcess.Length
                });
            }
            return true;
        }

        try
        {
            IPEndPoint endpoint;

            lock (_lock)
            {
                if (_clients.TryGetValue(clientId, out var client))
                {
                    endpoint = client.EndPoint;
                }
                else
                {
                    return false;
                }
            }

            var writer = PacketWriter.Get();
            try
            {
                writer.WriteByte((byte)LanPacketType.P2PData);
                writer.WriteID(_localClientId);

                byte[] dataToSend;
                if (length >= 0 && length < data.Length)
                {
                    dataToSend = new byte[length];
                    Array.Copy(data, 0, dataToSend, 0, length);
                }
                else
                {
                    dataToSend = [.. data];
                }
                writer.WriteBytes(dataToSend);

                var packetData = writer.GetBytes();
                _p2pListener.Send(packetData, packetData.Length, endpoint);
                return true;
            }
            finally
            {
                writer.Recycle();
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[LAN] SendP2PPacket error: {ex.Message}");
            return false;
        }
    }

    public bool ReadP2PPacket(Il2CppStructArray<byte> buffer, ref uint size, ref ID userId, int channel = 0)
    {
        lock (_packetQueue)
        {
            if (_packetQueue.Count == 0)
                return false;

            var packet = _packetQueue.Dequeue();
            int copyLength = Math.Min((int)packet.Size, buffer.Length);
            Array.Copy(packet.Data, 0, buffer, 0, copyLength);
            size = (uint)copyLength;
            userId = packet.SenderId;

            return true;
        }
    }

    // ===== Lobby Data Methods =====
    public string GetLobbyData(ID lobbyId, string pchKey)
    {
        lock (_lock)
        {
            if (_lobbyData.TryGetValue(lobbyId, out var data))
            {
                return data.TryGetValue(pchKey, out var value) ? value : string.Empty;
            }
            return string.Empty;
        }
    }

    public bool SetLobbyData(ID lobbyId, string pchKey, string pchValue)
    {
        lock (_lock)
        {
            // Only host can set lobby data
            if (!_isHost || _currentLobbyId != lobbyId)
                return false;

            // Update lobby data storage
            if (!_lobbyData.ContainsKey(lobbyId))
                _lobbyData[lobbyId] = [];

            _lobbyData[lobbyId][pchKey] = pchValue;

            // Update current lobby data struct
            switch (pchKey)
            {
                case ReplantedOnlineMod.Constants.MOD_VERSION_KEY:
                    _currentLobbyData.ModVersion = pchValue;
                    break;
                case ReplantedOnlineMod.Constants.GAME_CODE_KEY:
                    _currentLobbyData.GameCode = pchValue;
                    break;
                case "max_players":
                    if (int.TryParse(pchValue, out int maxPlayers))
                        _currentLobbyData.MaxPlayers = maxPlayers;
                    break;
                case "name":
                    _currentLobbyData.Name = pchValue;
                    break;
                case "joinable":
                    if (bool.TryParse(pchValue, out bool joinable))
                        _currentLobbyData.IsJoinable = joinable;
                    break;
            }

            // Broadcast update to all clients
            BroadcastLobbyDataUpdate(pchKey, pchValue);

            return true;
        }
    }

    public bool DeleteLobbyData(ID lobbyId, string pchKey)
    {
        lock (_lock)
        {
            if (!_isHost || !_lobbyData.TryGetValue(lobbyId, out var data))
                return false;

            return data.Remove(pchKey);
        }
    }

    public bool RequestLobbyData(ID lobbyId)
    {
        return true;
    }

    // ===== Lobby Member Data Methods =====
    public string GetLobbyMemberData(ID lobbyId, ID clientId, string pchKey)
    {
        lock (_lock)
        {
            if (_clientLobbyData.TryGetValue(lobbyId, out var lobbyClients) &&
                lobbyClients.TryGetValue(clientId, out var clientData))
            {
                return clientData.TryGetValue(pchKey, out var value) ? value : string.Empty;
            }
            return string.Empty;
        }
    }

    public void SetLobbyMemberData(ID lobbyId, string pchKey, string pchValue)
    {
        lock (_lock)
        {
            if (_currentLobbyId != lobbyId)
                return;

            // Initialize storage for this lobby if needed
            if (!_clientLobbyData.ContainsKey(lobbyId))
                _clientLobbyData[lobbyId] = [];

            // Initialize storage for this client if needed
            if (!_clientLobbyData[lobbyId].ContainsKey(_localClientId))
                _clientLobbyData[lobbyId][_localClientId] = [];

            // Update the data
            _clientLobbyData[lobbyId][_localClientId][pchKey] = pchValue;

            // Send update to host (who will broadcast to all)
            if (!_isHost)
            {
                SendClientLobbyDataUpdate(lobbyId, pchKey, pchValue);
            }
            else
            {
                // Host broadcasts to all clients
                BroadcastClientLobbyDataUpdate(_localClientId, pchKey, pchValue);
            }
        }
    }

    // ===== Lobby Member Management Methods =====
    public int GetNumLobbyMembers(ID lobbyId)
    {
        lock (_lock)
        {
            return _clients.Count(c => IsClientInLobby(c.Key, lobbyId));
        }
    }

    private bool IsClientInLobby(ID clientId, ID lobbyId)
    {
        return _clients.ContainsKey(clientId) && _currentLobbyId == lobbyId;
    }

    public ID GetLobbyMemberByIndex(ID lobbyId, int iMember)
    {
        lock (_lock)
        {
            var members = _clients.Keys.Where(c => IsClientInLobby(c, lobbyId)).ToList();

            if (iMember >= 0 && iMember < members.Count)
                return members[iMember];

            return ID.Null;
        }
    }

    public string GetMemberName(ID clientId)
    {
        if (clientId == _localClientId)
        {
            return _currentLobbyData.Name;
        }

        lock (_lock)
        {
            return _clients.TryGetValue(clientId, out var client) ? client.Name : "Unknown Player";
        }
    }

    public bool SetLobbyMemberLimit(ID lobbyId, int cMaxMembers)
    {
        return SetLobbyData(lobbyId, "max_players", cMaxMembers.ToString());
    }

    // ===== P2P Session Management Methods =====
    public bool AcceptP2PSessionWithUser(ID clientId)
    {
        try
        {
            var writer = PacketWriter.Get();
            writer.WriteByte((byte)LanPacketType.P2PSessionAccept);
            writer.WriteID(_localClientId);
            writer.WriteBytes([]);
            var packetData = writer.GetBytes();
            writer.Recycle();

            lock (_lock)
            {
                if (_clients.TryGetValue(clientId, out var client))
                {
                    _p2pListener.Send(packetData, packetData.Length, client.EndPoint);

                    if (_isHost && _currentLobbyId.HasValue)
                    {
                        Task.Run(() =>
                        {
                            // Send all lobby data to new member
                            foreach (var kvp in _lobbyData[_currentLobbyId])
                            {
                                SendLobbyDataItem(clientId, kvp.Key, kvp.Value);
                            }

                            // Send all member data to new member
                            SendAllMemberDataToClient(clientId);
                        });
                    }

                    return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public bool CloseP2PSessionWithUser(ID clientId)
    {
        try
        {
            var writer = PacketWriter.Get();
            writer.WriteByte((byte)LanPacketType.P2PSessionClose);
            writer.WriteID(_localClientId);
            writer.WriteBytes([]);
            var packetData = writer.GetBytes();
            writer.Recycle();

            lock (_lock)
            {
                if (_clients.TryGetValue(clientId, out var client))
                {
                    _p2pListener.Send(packetData, packetData.Length, client.EndPoint);

                    if (_currentLobbyId.HasValue)
                    {
                        _clients.Remove(clientId);
                        if (_clientLobbyData.TryGetValue(_currentLobbyId, out var lobbyClients))
                        {
                            lobbyClients.Remove(clientId);
                        }

                        NetLobby.OnLobbyMemberLeave(_currentLobbyData, clientId);
                    }

                    return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    // ===== Lobby Lifecycle Methods =====
    public void JoinLobby(ID lobbyId)
    {
        lock (_lock)
        {
            _currentLobbyId = lobbyId;
            _isHost = false;

            var hostId = GetLobbyOwner(lobbyId);
            if (hostId.HasValue)
            {
                SendP2PSessionRequest(hostId);
            }
        }
    }

    public void LeaveLobby(ID lobbyId)
    {
        lock (_lock)
        {
            if (_currentLobbyId == lobbyId)
            {
                if (!_isHost)
                {
                    var hostId = GetLobbyOwner(lobbyId);
                    if (hostId.HasValue)
                        CloseP2PSessionWithUser(hostId);
                }
                else
                {
                    StopHosting();
                }
            }
        }
    }

    public bool SetLobbyJoinable(ID lobbyId, bool bLobbyJoinable)
    {
        return SetLobbyData(lobbyId, "joinable", bLobbyJoinable.ToString());
    }

    public bool SetLobbyType(ID lobbyId, LobbyType eLobbyType)
    {
        return true;
    }

    public ID GetLobbyOwner(ID lobbyId)
    {
        if (_isHost && _currentLobbyId == lobbyId)
            return _localClientId;

        lock (_lock)
        {
            if (_discoveredLobbies.TryGetValue(lobbyId, out var presence))
                return presence.ServerId;
        }
        return ID.Null;
    }

    // ===== Helper Methods =====
    private void SendP2PSessionRequest(ID targetId)
    {
        try
        {
            var writer = PacketWriter.Get();
            writer.WriteByte((byte)LanPacketType.P2PSessionRequest);
            writer.WriteID(_localClientId);
            writer.WriteULong(_localClientId.AsULong());
            var packetData = writer.GetBytes();
            writer.Recycle();

            lock (_lock)
            {
                if (_clients.TryGetValue(targetId, out var client))
                {
                    _p2pListener.Send(packetData, packetData.Length, client.EndPoint);
                }
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[LAN] Failed to send session request: {ex.Message}");
        }
    }

    private void SendClientInfo(ID targetId)
    {
        try
        {
            var writer = PacketWriter.Get();
            writer.WriteByte((byte)LanPacketType.ClientInfo);
            writer.WriteID(_localClientId);
            writer.WriteString(_currentLobbyData.Name);
            var packetData = writer.GetBytes();
            writer.Recycle();

            lock (_lock)
            {
                if (_clients.TryGetValue(targetId, out var client))
                {
                    _p2pListener.Send(packetData, packetData.Length, client.EndPoint);
                    MelonLogger.Msg($"[LAN] Sent client info to {targetId}");
                }
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[LAN] Failed to send client info: {ex.Message}");
        }
    }

    private void BroadcastLobbyDataUpdate(string key, string value)
    {
        if (_isUpdatingLobbyData) return;

        _isUpdatingLobbyData = true;

        try
        {
            var writer = PacketWriter.Get();
            writer.WriteByte((byte)LanPacketType.LobbyData);
            writer.WriteID(_localClientId);
            writer.WriteString(key);
            writer.WriteString(value);
            var packetData = writer.GetBytes();
            writer.Recycle();

            lock (_lock)
            {
                foreach (var client in _clients.Values.Where(c => c.ClientId != _localClientId))
                {
                    try
                    {
                        _p2pListener.Send(packetData, packetData.Length, client.EndPoint);
                    }
                    catch { }
                }
            }

            // Trigger callback for host
            NetLobby.OnLobbyDataChanged(_currentLobbyData);
        }
        finally
        {
            _isUpdatingLobbyData = false;
        }
    }

    private void BroadcastClientLobbyDataUpdate(ID clientId, string key, string value)
    {
        if (_isUpdatingClientData) return;

        _isUpdatingClientData = true;

        try
        {
            var writer = PacketWriter.Get();
            writer.WriteByte((byte)LanPacketType.ClientLobbyData);
            writer.WriteID(clientId);
            writer.WriteString(key);
            writer.WriteString(value);
            var packetData = writer.GetBytes();
            writer.Recycle();

            lock (_lock)
            {
                foreach (var client in _clients.Values.Where(c => c.ClientId != _localClientId))
                {
                    try
                    {
                        _p2pListener.Send(packetData, packetData.Length, client.EndPoint);
                    }
                    catch { }
                }
            }

            // Update local storage for host
            HandleClientLobbyData(clientId, key, value);
        }
        finally
        {
            _isUpdatingClientData = false;
        }
    }

    private void BroadcastClientJoined(ID clientId, string clientName)
    {
        var writer = PacketWriter.Get();
        writer.WriteByte((byte)LanPacketType.ClientInfo);
        writer.WriteID(clientId);
        writer.WriteString(clientName);
        var packetData = writer.GetBytes();
        writer.Recycle();

        lock (_lock)
        {
            foreach (var client in _clients.Values.Where(c => c.ClientId != _localClientId && c.ClientId != clientId))
            {
                try
                {
                    _p2pListener.Send(packetData, packetData.Length, client.EndPoint);
                }
                catch { }
            }
        }
    }

    private void SendClientLobbyDataUpdate(ID lobbyId, string key, string value)
    {
        var hostId = GetLobbyOwner(lobbyId);
        if (!hostId.HasValue || hostId == _localClientId) return;

        var writer = PacketWriter.Get();
        writer.WriteByte((byte)LanPacketType.ClientLobbyData);
        writer.WriteID(_localClientId);
        writer.WriteString(key);
        writer.WriteString(value);
        var packetData = writer.GetBytes();
        writer.Recycle();

        lock (_lock)
        {
            if (_clients.TryGetValue(hostId, out var client))
            {
                try
                {
                    _p2pListener.Send(packetData, packetData.Length, client.EndPoint);
                }
                catch { }
            }
        }
    }

    private void SendLobbyDataItem(ID clientId, string key, string value)
    {
        var writer = PacketWriter.Get();
        writer.WriteByte((byte)LanPacketType.LobbyData);
        writer.WriteID(_localClientId);
        writer.WriteString(key);
        writer.WriteString(value);
        var packetData = writer.GetBytes();
        writer.Recycle();

        lock (_lock)
        {
            if (_clients.TryGetValue(clientId, out var client))
            {
                try
                {
                    _p2pListener.Send(packetData, packetData.Length, client.EndPoint);
                }
                catch { }
            }
        }
    }

    private void SendAllMemberDataToClient(ID clientId)
    {
        lock (_lock)
        {
            if (!_clientLobbyData.TryGetValue(_currentLobbyId, out var lobbyClients))
                return;

            foreach (var clientKvp in lobbyClients)
            {
                foreach (var dataKvp in clientKvp.Value)
                {
                    var writer = PacketWriter.Get();
                    writer.WriteByte((byte)LanPacketType.ClientLobbyData);
                    writer.WriteID(clientKvp.Key);
                    writer.WriteString(dataKvp.Key);
                    writer.WriteString(dataKvp.Value);
                    var packetData = writer.GetBytes();
                    writer.Recycle();

                    if (_clients.TryGetValue(clientId, out var client))
                    {
                        try
                        {
                            _p2pListener.Send(packetData, packetData.Length, client.EndPoint);
                        }
                        catch { }
                    }
                }
            }
        }
    }

    private void HandleP2PSessionRequest(ID senderId, IPEndPoint endpoint)
    {
        MelonLogger.Msg($"[LAN] P2P session request from {senderId}");

        lock (_lock)
        {
            // Store client info from session request
            if (!_clients.ContainsKey(senderId))
            {
                _clients[senderId] = new LanClientInfo
                {
                    ClientId = senderId,
                    EndPoint = endpoint,
                    Name = $"Player {senderId.AsULong():X8}",
                    LastSeen = DateTime.UtcNow
                };
            }
        }

        NetLobby.OnP2PSessionRequest(senderId);
    }

    private void HandleP2PSessionAccept(ID senderId)
    {
        MelonLogger.Msg($"[LAN] P2P session accepted by {senderId}");

        // After session is accepted, send our client info
        if (!_isHost && _currentLobbyId.HasValue)
        {
            var hostId = GetLobbyOwner(_currentLobbyId);
            if (hostId.HasValue)
            {
                SendClientInfo(hostId);
            }
        }
    }

    private void HandleP2PSessionClose(ID senderId)
    {
        MelonLogger.Msg($"[LAN] P2P session closed by {senderId}");

        lock (_lock)
        {
            _clients.Remove(senderId);
            if (_currentLobbyId.HasValue && _clientLobbyData.TryGetValue(_currentLobbyId, out var lobbyClients))
            {
                lobbyClients.Remove(senderId);
            }
        }
    }

    private void HandleClientInfo(ID senderId, PacketReader reader)
    {
        var clientName = reader.ReadString();

        lock (_lock)
        {
            // Update or add client info
            if (_clients.TryGetValue(senderId, out var client))
            {
                client.Name = clientName;
                client.LastSeen = DateTime.UtcNow;
            }
            else
            {
                _clients[senderId] = new LanClientInfo
                {
                    ClientId = senderId,
                    EndPoint = null, // We should have this from session request
                    Name = clientName,
                    LastSeen = DateTime.UtcNow
                };
            }

            MelonLogger.Msg($"[LAN] Client info received: {clientName} ({senderId})");

            // If we're the host, broadcast this new client to everyone and notify NetLobby
            if (_isHost && _currentLobbyId.HasValue)
            {
                // Broadcast to other clients
                BroadcastClientJoined(senderId, clientName);

                // Notify NetLobby that a member joined
                NetLobby.OnLobbyMemberJoined(_currentLobbyData, senderId);

                // Send all existing member data to the new client
                Task.Run(() => SendAllMemberDataToClient(senderId));
            }
        }
    }

    private void HandleLobbyData(ID senderId, PacketReader reader)
    {
        // Only accept lobby data from host
        if (!_isHost && senderId != GetLobbyOwner(_currentLobbyId))
            return;

        var key = reader.ReadString();
        var value = reader.ReadString();

        // Update storage
        if (_currentLobbyId.HasValue)
        {
            if (!_lobbyData.ContainsKey(_currentLobbyId))
                _lobbyData[_currentLobbyId] = [];

            _lobbyData[_currentLobbyId][key] = value;

            // Update current lobby data struct
            switch (key)
            {
                case ReplantedOnlineMod.Constants.MOD_VERSION_KEY:
                    _currentLobbyData.ModVersion = value;
                    break;
                case ReplantedOnlineMod.Constants.GAME_CODE_KEY:
                    _currentLobbyData.GameCode = value;
                    break;
                case "max_players":
                    if (int.TryParse(value, out int maxPlayers))
                        _currentLobbyData.MaxPlayers = maxPlayers;
                    break;
                case "name":
                    _currentLobbyData.Name = value;
                    break;
                case "joinable":
                    if (bool.TryParse(value, out bool joinable))
                        _currentLobbyData.IsJoinable = joinable;
                    break;
            }

            // Forward to NetLobby
            NetLobby.OnLobbyDataChanged(_currentLobbyData);
        }
    }

    private void HandleClientLobbyData(ID senderId, PacketReader reader)
    {
        var key = reader.ReadString();
        var value = reader.ReadString();
        HandleClientLobbyData(senderId, key, value);
    }

    private void HandleClientLobbyData(ID senderId, string key, string value)
    {
        if (!_currentLobbyId.HasValue)
            return;

        lock (_lock)
        {
            // Initialize storage for this lobby if needed
            if (!_clientLobbyData.ContainsKey(_currentLobbyId))
                _clientLobbyData[_currentLobbyId] = [];

            // Initialize storage for this client if needed
            if (!_clientLobbyData[_currentLobbyId].ContainsKey(senderId))
                _clientLobbyData[_currentLobbyId][senderId] = [];

            // Update the data
            _clientLobbyData[_currentLobbyId][senderId][key] = value;
        }

        // If we're the host and this isn't from a broadcast we initiated, broadcast to others
        if (_isHost && !_isUpdatingClientData)
        {
            BroadcastClientLobbyDataUpdate(senderId, key, value);
        }
    }
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _broadcastListener?.Close();
        _p2pListener?.Close();
    }
}

internal class LanClientInfo
{
    public ID ClientId { get; set; }
    public IPEndPoint EndPoint { get; set; }
    public string Name { get; set; }
    public DateTime LastSeen { get; set; }
}

internal class LanServerPresence
{
    public ID ServerId { get; set; }
    public string ServerName { get; set; }
    public int PlayerCount { get; set; }
    public int MaxPlayers { get; set; }
    public ID LobbyId { get; set; }
    public bool IsJoinable { get; set; }
    public string ModVersion { get; set; }
    public string GameCode { get; set; }
}

internal enum LanPacketType : byte
{
    P2PData = 0,
    P2PSessionRequest = 1,
    P2PSessionAccept = 2,
    P2PSessionClose = 3,
    LobbyData = 4,
    ClientLobbyData = 5,
    ClientInfo = 6
}

internal class PendingPacket
{
    public byte[] Data { get; set; }
    public ID SenderId { get; set; }
    public uint Size { get; set; }
}
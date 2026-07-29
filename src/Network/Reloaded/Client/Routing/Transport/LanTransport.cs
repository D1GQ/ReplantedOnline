using Il2CppSteamworks;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Modules.Reloaded.Panel;
using ReplantedOnline.MonoScripts.Unity;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Network.Reloaded.Server.Lan;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.MelonLoader;
using System.Net;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Transport;

/// <summary>
/// Provides LAN-based network transport functionality
/// </summary>
internal sealed class LanTransport : INetworkTransport
{
    /// <summary>
    /// Character set used for IPs.
    /// </summary>
    internal static readonly char[] IP_CHARS = "0123456789.:".ToCharArray();

    internal LanTransport()
    {
        LanServer.Server = new();
    }

    private bool _isJoining;
    private bool _hasJoined;

    public ID LocalClientId => LanServer.Server.LocalMemberId;

    public void Tick(float deltaTime) { }

    public async Task JoinFirstLanLobby()
    {
        if (_isJoining || _hasJoined) return;

        Transitions.SetLoading();

        try
        {
            _isJoining = true;
            ReplantedOnlineMod.Logger.Msg(typeof(LanTransport), "Searching for lobbies...");

            var serverData = await LanServer.Server.ServerBroadcast!.DiscoverFirstLobby();

            if (serverData == null)
            {
                ReplantedOnlineMod.Logger.Msg(typeof(LanTransport), "No lobbies found");
                _isJoining = false;
                ShowDisconnectPopup("No LAN lobbies found");
                return;
            }

            ReplantedOnlineMod.Logger.Msg(typeof(LanTransport), $"Found lobby: {serverData.GetServerName()}");
            JoinLobby(serverData.LobbyId);
            _hasJoined = true;
            _isJoining = false;
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(typeof(LanTransport), $"Error: {ex.Message}");
            _isJoining = false;
            ShowDisconnectPopup("Error joining LAN lobby");
        }
    }

    /// <summary>
    /// Joins a lobby directly by IP address and port
    /// </summary>
    /// <param name="ipAddress">The IP address of the host</param>
    /// <param name="port">The port the host is listening on</param>
    public async Task JoinByIP(string ipAddress, int port)
    {
        if (_isJoining || _hasJoined) return;
        Transitions.SetLoading();

        try
        {
            _isJoining = true;
            ReplantedOnlineMod.Logger.Msg(typeof(LanTransport), $"Attempting to join lobby at {ipAddress}:{port}");

            if (!IPAddress.TryParse(ipAddress, out var ip))
            {
                ReplantedOnlineMod.Logger.Error(typeof(LanTransport), $"Invalid IP address: {ipAddress}");
                _isJoining = false;
                ShowDisconnectPopup("Invalid IP address");
                return;
            }

            if (port < 1 || port > 65535)
            {
                ReplantedOnlineMod.Logger.Error(typeof(LanTransport), $"Invalid port: {port}");
                _isJoining = false;
                ShowDisconnectPopup("Invalid port number");
                return;
            }

            var endpoint = new IPEndPoint(ip, port);

            // Create server data from the endpoint
            var serverData = new LanServerData
            {
                HostAddress = ip,
                GamePort = port,
                LobbyId = ID.CreateRandomULong(), // Will be updated during handshake
                HostId = new ID(endpoint, IdType.IPEndPoint)
            };
            serverData.SetServerName($"Direct Connection to {ipAddress}");
            serverData.SetModVersion(ReplantedOnlineMod.ModInfo.MOD_VERSION_FORMATTED);

            // Start client and connect
            string name;
            if (ReloadedLobby.TransportMode == TransportMode.Lan)
            {
                name = "Client";
            }
            else
            {
                try
                {
                    name = SteamFriends.Internal.GetPersonaName();
                }
                catch
                {
                    name = "Client";
                }
            }
            LanServer.StartClient(name);

            // Set up handshake completion
            var handshakeTask = LanServer.Server.JoinServer(serverData);

            // Wait for handshake with timeout
            var timeout = Task.Delay(5000);
            var completed = await Task.WhenAny(handshakeTask, timeout);

            if (completed == timeout)
            {
                ReplantedOnlineMod.Logger.Error(typeof(LanTransport), "Connection timed out");
                _isJoining = false;
                LanServer.Leave();
                ShowDisconnectPopup("Connection timed out");
                return;
            }

            var success = await handshakeTask;
            if (!success)
            {
                var reason = LanServer.Server.RejectionReasons.TryGetValue(LanServer.Server.ServerData.HostId, out var rejectionReason)
                    ? rejectionReason
                    : "Connection rejected";
                ReplantedOnlineMod.Logger.Error(typeof(LanTransport), $"Connection rejected: {reason}");
                _isJoining = false;
                LanServer.Leave();
                ShowDisconnectPopup($"Connection rejected: {reason}");
                return;
            }

            _hasJoined = true;
            _isJoining = false;
            ReplantedOnlineMod.Logger.Msg(typeof(LanTransport), $"Successfully joined lobby at {ipAddress}:{port}");
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(typeof(LanTransport), $"Error joining by IP: {ex.Message}");
            _isJoining = false;
            LanServer.Leave();
            ShowDisconnectPopup($"Error joining: {ex.Message}");
        }
    }

    /// <summary>
    /// Joins a lobby directly by IP:Port string
    /// </summary>
    /// <param name="ipAndPort">The IP:Port string (e.g., "192.168.1.100:14242")</param>
    public async Task JoinByIPString(string ipAndPort)
    {
        var parts = ipAndPort.Split(':');
        if (parts.Length != 2)
        {
            ReplantedOnlineMod.Logger.Error(typeof(LanTransport), $"Invalid IP:Port format: {ipAndPort}");
            ShowDisconnectPopup("Invalid format. Use IP:Port (e.g., 192.168.1.100:14242)");
            return;
        }

        if (!int.TryParse(parts[1], out var port))
        {
            ReplantedOnlineMod.Logger.Error(typeof(LanTransport), $"Invalid port in: {ipAndPort}");
            ShowDisconnectPopup("Invalid port number");
            return;
        }

        await JoinByIP(parts[0], port);
    }

    private static void ShowDisconnectPopup(string message)
    {
        MainThreadDispatcher.Execute(() =>
        {
            Transitions.ToMainMenu(() =>
            {
                CustomPopupPanel.Show("Disconnected", message);
            });
        });
    }

    // ===== P2P Packet Methods =====
    public bool IsP2PPacketAvailable(out uint msgSize, PacketChannel channel = PacketChannel.Main)
    {
        msgSize = 0;
        return LanServer.Server.IsP2PPacketAvailable(ref msgSize, channel);
    }

    public bool SendP2PPacket(ID clientId, byte[] data, PacketChannel channel = PacketChannel.Main, P2PSend sendType = P2PSend.Reliable)
    {
        return LanServer.Server.SendP2PPacket(clientId, data, channel);
    }

    public bool ReadP2PPacket(PacketBuffer buffer, PacketChannel channel = PacketChannel.Main)
    {
        return LanServer.Server.ReadP2PPacket(buffer, channel);
    }

    // ===== Lobby Data Methods =====
    public string GetLobbyData(ID lobbyId, string pchKey)
    {
        if (LanServer.Server.ServerData?.Data.TryGetValue(pchKey, out var value) == true)
        {
            return value;
        }

        return string.Empty;
    }

    public bool SetLobbyData(ID lobbyId, string pchKey, string pchValue)
    {
        if (!LanServer.Server.IsHost) return false;
        LanServer.Server.SetLobbyData(pchKey, pchValue);
        return true;
    }

    public bool DeleteLobbyData(ID lobbyId, string pchKey)
    {
        if (!LanServer.Server.IsHost) return false;
        LanServer.Server.SetLobbyData(pchKey, string.Empty, true);
        return true;
    }

    public bool RequestLobbyData(ID lobbyId)
    {
        return true;
    }

    // ===== Lobby Member Data Methods =====
    public string GetLobbyMemberData(ID lobbyId, ID clientId, string pchKey)
    {
        if (LanServer.Server.Members.TryGetValue(clientId, out var client))
        {
            return client.Data.TryGetValue(pchKey, out var value) ? value : string.Empty;
        }
        return string.Empty;
    }

    public void SetLobbyMemberData(ID lobbyId, string pchKey, string pchValue)
    {
        LanServer.Server.SetMemberData(pchKey, pchValue);
    }

    // ===== Lobby Member Management Methods =====
    public int GetNumLobbyMembers(ID lobbyId)
    {
        return LanServer.Server.Members.Count;
    }

    public ID GetLobbyMemberByIndex(ID lobbyId, int memberIndex)
    {
        var clients = LanServer.Server.Members.Values.ToList();
        return memberIndex >= 0 && memberIndex < clients.Count
            ? clients[memberIndex].MemberId
            : ID.Null;
    }

    public string GetMemberName(ID clientId)
    {
        if (LanServer.Server.Members.TryGetValue(clientId, out var client))
        {
            return client.PlayerName;
        }

        return "???";
    }

    public bool SetLobbyMemberLimit(ID lobbyId, int maxMembers)
    {
        if (!LanServer.Server.IsHost) return false;
        LanServer.Server.ServerData?.SetMaxPlayerCount(maxMembers);
        return true;
    }

    // ===== P2P Session Management Methods =====
    public bool AcceptP2PSessionWithUser(ID clientId)
    {
        if (LanServer.Server.PendingRequests.Contains(clientId))
        {
            LanServer.Server.PendingRequests.Remove(clientId);
            return true;
        }
        return false;
    }

    public bool CloseP2PSessionWithUser(ID clientId)
    {
        if (LanServer.Server.Members.TryGetValue(clientId, out var client))
        {
            LanServer.Server.RemoveMember(client);
            return true;
        }
        return false;
    }

    // ===== Lobby Lifecycle Methods =====
    public void CreateLobby(int maxPlayers)
    {
        string name;
        if (ReloadedLobby.TransportMode == TransportMode.Lan)
        {
            name = "Host";
        }
        else
        {
            try
            {
                name = SteamFriends.Internal.GetPersonaName();
            }
            catch
            {
                name = "Host";
            }
        }
        LanServer.StartHost(name, maxPlayers);
    }

    public void JoinLobby(ID lobbyId)
    {
        var serverData = LanServer.Server.ServerBroadcast!.GetDiscoveredServer(lobbyId);
        if (serverData != null)
        {
            LanServer.StartClient("Client");
            _ = LanServer.Server.JoinServer(serverData);
        }
    }

    public void LeaveLobby(ID lobbyId)
    {
        _hasJoined = false;
        _isJoining = false;
        LanServer.Leave();
    }

    public bool SetLobbyJoinable(ID lobbyId, bool lobbyJoinable)
    {
        if (!LanServer.Server.IsHost) return false;
        LanServer.Server.ServerData?.SetIsJoinable(lobbyJoinable);
        return true;
    }

    public bool SetLobbyType(ID lobbyId, LobbyType lobbyType)
    {
        return true;
    }

    public ID GetLobbyOwner(ID lobbyId)
    {
        return LanServer.Server.ServerData.HostId;
    }

    public void Dispose()
    {
        _hasJoined = false;
        _isJoining = false;
        LanServer.Leave();
        LanServer.Server.Dispose();
    }
}
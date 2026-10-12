using Il2CppSteamworks;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Modules.Reloaded.Panel;
using ReplantedOnline.MonoScripts.Unity;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Network.Reloaded.Server;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.MelonLoader;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Transport;

/// <summary>
/// Provides IP-based network transport functionality.
/// Supports LAN discovery, direct IP/hostname connections, and dedicated hosting.
/// </summary>
internal sealed class ReloadedTransport : INetworkTransport
{
    /// <summary>
    /// Whether this transport should advertise hosted lobbies on the LAN.
    /// Set at construction time; controls default behavior of <see cref="CreateLobby(int)"/>.
    /// </summary>
    internal bool BroadcastOnLan { get; }

    /// <summary>
    /// Creates a new transport.
    /// </summary>
    internal ReloadedTransport(bool enableBroadcast)
    {
        BroadcastOnLan = enableBroadcast;
        ReloadedServer.Server = new ReloadedServer(enableBroadcast);
    }

    private bool _isJoining;
    private bool _hasJoined;

    public ID LocalClientId => ReloadedServer.Server.LocalMemberId;

    public void Tick(float deltaTime) { }

    // ... JoinFirstLanLobby, JoinDirectAsync, ShowDisconnectPopup, and all
    //     P2P/Lobby/Member methods unchanged from your current version ...

    // ===== P2P Packet Methods =====
    public bool IsP2PPacketAvailable(out uint msgSize, PacketChannel channel = PacketChannel.Main)
    {
        msgSize = 0;
        return ReloadedServer.Server.IsP2PPacketAvailable(ref msgSize, channel);
    }

    public bool SendP2PPacket(ID clientId, byte[] data, PacketChannel channel = PacketChannel.Main, P2PSend sendType = P2PSend.Reliable)
    {
        return ReloadedServer.Server.SendP2PPacket(clientId, data, channel);
    }

    public bool ReadP2PPacket(PacketBuffer buffer, PacketChannel channel = PacketChannel.Main)
    {
        return ReloadedServer.Server.ReadP2PPacket(buffer, channel);
    }

    // ===== Lobby Data Methods =====
    public string GetLobbyData(ID lobbyId, string pchKey)
    {
        if (ReloadedServer.Server.ServerData?.Data.TryGetValue(pchKey, out var value) == true)
        {
            return value;
        }
        return string.Empty;
    }

    public bool SetLobbyData(ID lobbyId, string pchKey, string pchValue)
    {
        if (!ReloadedServer.Server.IsHost) return false;
        ReloadedServer.Server.SetLobbyData(pchKey, pchValue);
        return true;
    }

    public bool DeleteLobbyData(ID lobbyId, string pchKey)
    {
        if (!ReloadedServer.Server.IsHost) return false;
        ReloadedServer.Server.SetLobbyData(pchKey, string.Empty, true);
        return true;
    }

    public bool RequestLobbyData(ID lobbyId) => true;

    // ===== Lobby Member Data Methods =====
    public string GetLobbyMemberData(ID lobbyId, ID clientId, string pchKey)
    {
        if (ReloadedServer.Server.Members.TryGetValue(clientId, out var client))
        {
            return client.Data.TryGetValue(pchKey, out var value) ? value : string.Empty;
        }
        return string.Empty;
    }

    public void SetLobbyMemberData(ID lobbyId, string pchKey, string pchValue)
    {
        ReloadedServer.Server.SetMemberData(pchKey, pchValue);
    }

    // ===== Lobby Member Management Methods =====
    public int GetNumLobbyMembers(ID lobbyId) => ReloadedServer.Server.Members.Count;

    public ID GetLobbyMemberByIndex(ID lobbyId, int memberIndex)
    {
        var clients = ReloadedServer.Server.Members.Values.ToList();
        return memberIndex >= 0 && memberIndex < clients.Count
            ? clients[memberIndex].MemberId
            : ID.Null;
    }

    public string GetMemberName(ID clientId)
    {
        if (ReloadedServer.Server.Members.TryGetValue(clientId, out var client))
        {
            return client.PlayerName;
        }
        return "???";
    }

    public bool SetLobbyMemberLimit(ID lobbyId, int maxMembers)
    {
        if (!ReloadedServer.Server.IsHost) return false;
        ReloadedServer.Server.ServerData?.SetMaxPlayerCount(maxMembers);
        return true;
    }

    // ===== P2P Session Management Methods =====
    public bool AcceptP2PSessionWithUser(ID clientId)
    {
        if (ReloadedServer.Server.PendingRequests.Contains(clientId))
        {
            ReloadedServer.Server.PendingRequests.Remove(clientId);
            return true;
        }
        return false;
    }

    public bool CloseP2PSessionWithUser(ID clientId)
    {
        if (ReloadedServer.Server.Members.TryGetValue(clientId, out var client))
        {
            ReloadedServer.Server.RemoveMember(client);
            return true;
        }
        return false;
    }

    // ===== Lobby Lifecycle Methods =====

    public void CreateLobby(int maxPlayers)
    {
        ReloadedServer.StartHost(maxPlayers, BroadcastOnLan);
    }

    public void JoinLobby(ID lobbyId)
    {
        var broadcast = ReloadedServer.Server.ServerBroadcast;
        if (broadcast == null)
        {
            ReplantedOnlineMod.Logger.Error(typeof(ReloadedTransport), "Cannot join by lobby ID: LAN broadcast is disabled.");
            return;
        }

        var serverData = broadcast.GetDiscoveredServer(lobbyId);
        if (serverData != null)
        {
            ReloadedServer.StartClient(ConnectionMode.LanClient);
            _ = ReloadedServer.Server.JoinServer(serverData);
        }
    }

    public void LeaveLobby(ID lobbyId)
    {
        _hasJoined = false;
        _isJoining = false;
        ReloadedServer.Leave();
    }

    public bool SetLobbyJoinable(ID lobbyId, bool lobbyJoinable)
    {
        if (!ReloadedServer.Server.IsHost) return false;
        ReloadedServer.Server.ServerData?.SetIsJoinable(lobbyJoinable);
        return true;
    }

    public bool SetLobbyType(ID lobbyId, LobbyType lobbyType) => true;

    public ID GetLobbyOwner(ID lobbyId) => ReloadedServer.Server.ServerData.HostId;

    public void Dispose()
    {
        _hasJoined = false;
        _isJoining = false;
        ReloadedServer.Leave();
        ReloadedServer.Server.Dispose();
    }

    // ---- JoinFirstLanLobby / JoinDirectAsync (unchanged, kept for context) ----

    public async Task JoinFirstLanLobby()
    {
        if (_isJoining || _hasJoined) return;
        Transitions.SetLoading();
        try
        {
            _isJoining = true;
            ReplantedOnlineMod.Logger.Msg(typeof(ReloadedTransport), "Searching for lobbies...");

            var broadcast = ReloadedServer.Server.ServerBroadcast;
            if (broadcast == null)
            {
                ReplantedOnlineMod.Logger.Error(typeof(ReloadedTransport), "LAN broadcast is disabled on this transport instance.");
                _isJoining = false;
                ShowDisconnectPopup("$POPUP_NO_LAN_LOBBIES_FOUND");
                return;
            }

            var serverData = await broadcast.DiscoverFirstLobby();
            if (serverData == null)
            {
                ReplantedOnlineMod.Logger.Msg(typeof(ReloadedTransport), "No lobbies found");
                _isJoining = false;
                ShowDisconnectPopup("$POPUP_NO_LAN_LOBBIES_FOUND");
                return;
            }

            ReplantedOnlineMod.Logger.Msg(typeof(ReloadedTransport), $"Found lobby: {serverData.GetServerName()}");
            JoinLobby(serverData.LobbyId);
            _hasJoined = true;
            _isJoining = false;
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(typeof(ReloadedTransport), $"Error: {ex.Message}");
            ReloadedServer.AbortClient();
            _isJoining = false;
            ShowDisconnectPopup("$POPUP_ERROR_JOINING_LAN_LOBBY");
        }
    }

    public async Task JoinDirectAsync(string host, int port)
    {
        if (_isJoining || _hasJoined)
            return;

        Transitions.SetLoading();
        try
        {
            _isJoining = true;
            ReplantedOnlineMod.Logger.Msg(typeof(ReloadedTransport), $"Connecting to {host}:{port}...");

            ReloadedServer.StartClient(ConnectionMode.DirectClient);
            bool ok = await ReloadedServer.Server.JoinServerDirect(host, port);

            if (!ok)
            {
                ReplantedOnlineMod.Logger.Msg(typeof(ReloadedTransport), $"Failed to join {host}:{port}");
                ReloadedServer.AbortClient();
                _isJoining = false;
                ShowDisconnectPopup("$POPUP_ERROR_JOINING_IP_LOBBY");
            }

            _hasJoined = true;
            _isJoining = false;
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(typeof(ReloadedTransport), $"Error: {ex.Message}");
            ReloadedServer.AbortClient();
            _isJoining = false;
            ShowDisconnectPopup("$POPUP_ERROR_JOINING_IP_LOBBY");
        }
    }

    private static void ShowDisconnectPopup(string localizationKey)
    {
        MainThreadDispatcher.Execute(() =>
        {
            Transitions.ToMainMenu(() =>
            {
                CustomPopupPanel.ShowLocalized("$POPUP_HEADER_DISCONNECTED", localizationKey);
            });
        });
    }
}
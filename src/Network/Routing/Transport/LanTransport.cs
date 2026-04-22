using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSteamworks;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules;
using ReplantedOnline.Modules.Panel;
using ReplantedOnline.Monos;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Server.LAN;
using ReplantedOnline.Structs;

namespace ReplantedOnline.Network.Routing.Transport;

/// <summary>
/// Provides LAN-based network transport functionality
/// </summary>
internal sealed class LanTransport : INetworkTransport
{
    internal LanTransport()
    {
        LanServer.Server = new();
    }

    private bool _isJoining;
    private bool _hasJoined;

    public ID LocalClientId => LanServer.Server.LocalClientId;

    public void Tick(float deltaTime) { }

    public async Task JoinFirstLanLobby()
    {
        if (_isJoining || _hasJoined) return;

        try
        {
            _isJoining = true;
            ReplantedOnlineMod.Logger.Msg("[LanTransport] Searching for lobbies...");

            var serverData = await LanServer.Server.ServerBroadcast.DiscoverFirstLobby();

            if (serverData == null)
            {
                ReplantedOnlineMod.Logger.Msg("[LanTransport] No lobbies found");
                _isJoining = false;
                ShowDisconnectPopup("No LAN lobbies found");
                return;
            }

            ReplantedOnlineMod.Logger.Msg($"[LanTransport] Found lobby: {serverData.GetServerName()}");
            JoinLobby(serverData.LobbyId);
            _hasJoined = true;
            _isJoining = false;
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error($"[LanTransport] Error: {ex.Message}");
            _isJoining = false;
            ShowDisconnectPopup("Error joining LAN lobby");
        }
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

    public bool SendP2PPacket(ID clientId, Il2CppStructArray<byte> data, int length = -1, PacketChannel channel = PacketChannel.Main, P2PSend sendType = P2PSend.Reliable)
    {
        return LanServer.Server.SendP2PPacket(clientId, data, channel);
    }

    public bool ReadP2PPacket(P2PPacketBuffer buffer, PacketChannel channel = PacketChannel.Main)
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
        if (LanServer.Server.Clients.TryGetValue(clientId, out var client))
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
        return LanServer.Server.Clients.Count;
    }

    public ID GetLobbyMemberByIndex(ID lobbyId, int memberIndex)
    {
        var clients = LanServer.Server.Clients.Values.ToList();
        return memberIndex >= 0 && memberIndex < clients.Count
            ? clients[memberIndex].ClientId
            : ID.Null;
    }

    public string GetMemberName(ID clientId)
    {
        if (LanServer.Server.Clients.TryGetValue(clientId, out var client))
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
        if (LanServer.Server.Clients.TryGetValue(clientId, out var client))
        {
            LanServer.Server.RemoveClient(client);
            return true;
        }
        return false;
    }

    // ===== Lobby Lifecycle Methods =====
    public void CreateLobby(int maxPlayers)
    {
        LanServer.StartHost("Host", maxPlayers);
    }

    public void JoinLobby(ID lobbyId)
    {
        var serverData = LanServer.Server.ServerBroadcast.GetDiscoveredServer(lobbyId);
        if (serverData != null)
        {
            LanServer.StartClient("Client");
            LanServer.Server.JoinServer(serverData);
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
        LanServer.Server = null;
    }
}
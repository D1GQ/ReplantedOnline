using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSteamworks;
using Il2CppSteamworks.Data;
using ReplantedOnline.Interfaces;
using ReplantedOnline.Structs;

namespace ReplantedOnline.Network.Server.Transport;

internal sealed class LanTransport : INetworkTransport
{
    public ID LocalClientId => throw new NotImplementedException();

    public void Tick(float deltaTime)
    {
        throw new NotImplementedException();
    }

    // ===== P2P Packet Methods =====
    public bool IsP2PPacketAvailable(out uint msgSize, int channel = 0)
    {
        throw new NotImplementedException();
    }

    public bool SendP2PPacket(ID clientId, Il2CppStructArray<byte> data, int length = -1, int nChannel = 0, P2PSend sendType = P2PSend.Reliable)
    {
        throw new NotImplementedException();
    }

    public bool ReadP2PPacket(Il2CppStructArray<byte> buffer, ref uint size, ref ID userId, int channel = 0)
    {
        throw new NotImplementedException();
    }

    // ===== Lobby Data Methods =====
    public string GetLobbyData(ID lobbyId, string pchKey)
    {
        throw new NotImplementedException();
    }

    public bool SetLobbyData(ID lobbyId, string pchKey, string pchValue)
    {
        throw new NotImplementedException();
    }

    public bool DeleteLobbyData(ID lobbyId, string pchKey)
    {
        throw new NotImplementedException();
    }

    public bool RequestLobbyData(ID lobbyId)
    {
        throw new NotImplementedException();
    }

    // ===== Lobby Member Data Methods =====
    public string GetLobbyMemberData(ID lobbyId, ID clientId, string pchKey)
    {
        throw new NotImplementedException();
    }

    public void SetLobbyMemberData(ID lobbyId, string pchKey, string pchValue)
    {
        throw new NotImplementedException();
    }

    // ===== Lobby Member Management Methods =====
    public int GetNumLobbyMembers(ID lobbyId)
    {
        throw new NotImplementedException();
    }

    public ID GetLobbyMemberByIndex(ID lobbyId, int iMember)
    {
        throw new NotImplementedException();
    }

    public string GetMemberName(ID clientId)
    {
        throw new NotImplementedException();
    }

    public bool SetLobbyMemberLimit(ID lobbyId, int cMaxMembers)
    {
        throw new NotImplementedException();
    }

    // ===== P2P Session Management Methods =====
    public bool AcceptP2PSessionWithUser(ID clientId)
    {
        throw new NotImplementedException();
    }

    public bool CloseP2PSessionWithUser(ID clientId)
    {
        throw new NotImplementedException();
    }

    // ===== Lobby Lifecycle Methods =====
    public void JoinLobby(ID lobbyId)
    {
        throw new NotImplementedException();
    }

    public void LeaveLobby(ID lobbyId)
    {
        throw new NotImplementedException();
    }

    public bool SetLobbyJoinable(ID lobbyId, bool bLobbyJoinable)
    {
        throw new NotImplementedException();
    }

    public bool SetLobbyType(ID lobbyId, LobbyType eLobbyType)
    {
        throw new NotImplementedException();
    }

    public ID GetLobbyOwner(ID lobbyId)
    {
        throw new NotImplementedException();
    }

    // ===== Lobby Event Methods =====
    public void OnLobbyCreatedCompleted(Result result, Lobby data)
    {
        throw new NotImplementedException();
    }

    public void OnLobbyEnteredCompleted(Lobby data)
    {
        throw new NotImplementedException();
    }

    public void OnLobbyDataChanged(Lobby lobby)
    {
        throw new NotImplementedException();
    }

    public void OnLobbyMemberJoined(Lobby lobby, ID clientId)
    {
        throw new NotImplementedException();
    }

    public void OnLobbyMemberLeave(Lobby lobby, ID user)
    {
        throw new NotImplementedException();
    }

    public void OnP2PSessionRequest(ID clientId)
    {
        throw new NotImplementedException();
    }

    public void OnP2PSessionConnectFail(ID clientId, P2PSessionError error)
    {
        throw new NotImplementedException();
    }
}

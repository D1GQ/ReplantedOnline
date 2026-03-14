using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSteamworks;
using ReplantedOnline.Enums;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Server.Packet;
using ReplantedOnline.Patches.Steam;
using ReplantedOnline.Structs;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Network.Server.Transport;

/// <summary>
/// Provides Steamworks-based network transport functionality
/// </summary>
internal sealed class SteamTransport : INetworkTransport
{
    public ID LocalClientId => SteamUser.Internal.GetSteamID();

    public void Tick(float deltaTime)
    {
    }

    // ===== P2P Packet Methods =====
    public bool IsP2PPacketAvailable(out uint msgSize, PacketChannel channel = PacketChannel.Main)
    {
        msgSize = 0;
        return SteamNetworking.Internal.IsP2PPacketAvailable(ref msgSize, (int)channel);
    }

    public bool SendP2PPacket(ID clientId, Il2CppStructArray<byte> data, int length = -1, PacketChannel channel = PacketChannel.Main, P2PSend sendType = P2PSend.Reliable)
    {
        if (clientId.TryGetSteamId(out SteamId steamId))
            return SteamNetworking.SendP2PPacket(steamId, data, length, (int)channel, sendType);
        throw new ArgumentException("SendP2PPacket requires a SteamId");
    }

    public bool ReadP2PPacket(P2PPacketBuffer buffer, PacketChannel channel = PacketChannel.Main)
    {
        SteamId steamId = default;
        var result = SteamNetworking.ReadP2PPacket(buffer.Data, ref buffer.Size, ref steamId, (int)channel);
        if (result)
            buffer.ClientId = steamId.AsID();
        return result;
    }

    // ===== Lobby Data Methods =====
    public string GetLobbyData(ID lobbyId, string pchKey)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
            return SteamMatchmaking.Internal.GetLobbyData(id, pchKey);
        throw new ArgumentException("GetLobbyData requires a SteamId");
    }

    public bool SetLobbyData(ID lobbyId, string pchKey, string pchValue)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
            return SteamMatchmaking.Internal.SetLobbyData(id, pchKey, pchValue);
        throw new ArgumentException("SetLobbyData requires a SteamId");
    }

    public bool DeleteLobbyData(ID lobbyId, string pchKey)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
            return SteamMatchmaking.Internal.DeleteLobbyData(id, pchKey);
        throw new ArgumentException("DeleteLobbyData requires a SteamId");
    }

    public bool RequestLobbyData(ID lobbyId)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
            return SteamMatchmaking.Internal.RequestLobbyData(id);
        throw new ArgumentException("RequestLobbyData requires a SteamId");
    }

    // ===== Lobby Member Data Methods =====
    public string GetLobbyMemberData(ID lobbyId, ID clientId, string pchKey)
    {
        if (lobbyId.TryGetSteamId(out SteamId lid) && clientId.TryGetSteamId(out SteamId cid))
            return SteamMatchmaking.Internal.GetLobbyMemberData(lid, cid, pchKey);
        throw new ArgumentException("GetLobbyMemberData requires SteamIds");
    }

    public void SetLobbyMemberData(ID lobbyId, string pchKey, string pchValue)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
            SteamMatchmaking.Internal.SetLobbyMemberData(id, pchKey, pchValue);
        else
            throw new ArgumentException("SetLobbyMemberData requires a SteamId");
    }

    // ===== Lobby Member Management Methods =====
    public int GetNumLobbyMembers(ID lobbyId)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
            return SteamMatchmaking.Internal.GetNumLobbyMembersOriginal(id);
        throw new ArgumentException("GetNumLobbyMembers requires a SteamId");
    }

    public ID GetLobbyMemberByIndex(ID lobbyId, int memberIndex)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
        {
            var member = SteamMatchmaking.Internal.GetLobbyMemberByIndexOriginal(id, memberIndex);
            return member.AsID();
        }
        throw new ArgumentException("GetLobbyMemberByIndex requires a SteamId");
    }

    public string GetMemberName(ID clientId)
    {
        if (clientId.TryGetSteamId(out SteamId id))
            return SteamFriends.Internal.GetFriendPersonaName(id);
        throw new ArgumentException("GetMemberName requires a SteamId");
    }

    public bool SetLobbyMemberLimit(ID lobbyId, int maxMembers)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
            return SteamMatchmaking.Internal.SetLobbyMemberLimit(id, maxMembers);
        throw new ArgumentException("SetLobbyMemberLimit requires a SteamId");
    }

    // ===== P2P Session Management Methods =====
    public bool AcceptP2PSessionWithUser(ID clientId)
    {
        if (clientId.TryGetSteamId(out SteamId id))
            return SteamNetworking.Internal.AcceptP2PSessionWithUser(id);
        throw new ArgumentException("AcceptP2PSessionWithUser requires a SteamId");
    }

    public bool CloseP2PSessionWithUser(ID clientId)
    {
        if (clientId.TryGetSteamId(out SteamId id))
            return SteamNetworking.Internal.CloseP2PSessionWithUser(id);
        throw new ArgumentException("CloseP2PSessionWithUser requires a SteamId");
    }

    // ===== Lobby Lifecycle Methods =====
    public void CreateLobby(int maxPlayers)
    {
        SteamMatchmaking.CreateLobbyAsync(maxPlayers);
    }

    public void JoinLobby(ID lobbyId)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
            SteamMatchmaking.JoinLobbyAsync(id);
        else
            throw new ArgumentException("JoinLobby requires a SteamId");
    }

    public void LeaveLobby(ID lobbyId)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
            SteamMatchmaking.Internal.LeaveLobby(id);
        else
            throw new ArgumentException("LeaveLobby requires a SteamId");
    }

    public bool SetLobbyJoinable(ID lobbyId, bool lobbyJoinable)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
            return SteamMatchmaking.Internal.SetLobbyJoinable(id, lobbyJoinable);
        throw new ArgumentException("SetLobbyJoinable requires a SteamId");
    }

    public bool SetLobbyType(ID lobbyId, LobbyType lobbyType)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
            return SteamMatchmaking.Internal.SetLobbyType(id, lobbyType);
        throw new ArgumentException("SetLobbyType requires a SteamId");
    }

    public ID GetLobbyOwner(ID lobbyId)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
        {
            var owner = SteamMatchmaking.Internal.GetLobbyOwner(id);
            return owner.AsID();
        }
        throw new ArgumentException("GetLobbyOwner requires a SteamId");
    }

    public void Dispose()
    {
    }
}
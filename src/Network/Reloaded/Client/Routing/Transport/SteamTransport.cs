using Il2CppSteamworks;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.Modded;
using System.Collections.Concurrent;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Transport;

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

    public unsafe bool SendP2PPacket(ID clientId, byte[] data, PacketChannel channel = PacketChannel.Main, P2PSend sendType = P2PSend.Reliable)
    {
        if (!clientId.TryGetSteamId(out SteamId steamId))
        {
            throw new ArgumentException("SendP2PPacket requires a SteamId");
        }

        unsafe
        {
            fixed (byte* ptr = data)
            {
                return SteamNetworking.SendP2PPacket(steamId, ptr, (uint)data.Length, (int)channel, sendType);
            }
        }
    }

    public unsafe bool ReadP2PPacket(PacketBuffer buffer, PacketChannel channel = PacketChannel.Main)
    {
        if (buffer.Data == null || buffer.Data.Length == 0)
            return false;

        SteamId steamId = default;
        uint actualSize = 0;
        uint capacity = (uint)buffer.Data.Length;

        bool result;
        fixed (byte* ptr = buffer.Data)
        {
            result = SteamNetworking.ReadP2PPacket(ptr, capacity, ref actualSize, ref steamId, (int)channel);
        }

        if (result)
        {
            buffer.Size = actualSize;
            buffer.ClientId = steamId.AsID();
        }

        return result;
    }

    // ===== Lobby Data Methods =====
    internal static readonly ConcurrentDictionary<string, string> SteamLobbyDataCatched = [];
    public string GetLobbyData(ID lobbyId, string pchKey)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
        {
            if (!SteamLobbyDataCatched.TryGetValue(pchKey, out var value))
            {
                value = SteamMatchmaking.Internal.GetLobbyData(id, pchKey);
                SteamLobbyDataCatched[pchKey] = value;
            }

            return value;
        }

        throw new ArgumentException("GetLobbyData requires a SteamId");
    }

    public bool SetLobbyData(ID lobbyId, string pchKey, string pchValue)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
        {
            SteamLobbyDataCatched[pchKey] = pchValue;
            return SteamMatchmaking.Internal.SetLobbyData(id, pchKey, pchValue);
        }

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
    internal static readonly ConcurrentDictionary<SteamId, Dictionary<string, string>> SteamLobbyMemberDataCatched = [];
    public string GetLobbyMemberData(ID lobbyId, ID clientId, string pchKey)
    {
        if (lobbyId.TryGetSteamId(out SteamId lid) && clientId.TryGetSteamId(out SteamId cid))
        {
            if (!SteamLobbyMemberDataCatched.TryGetValue(cid, out var memberData))
            {
                memberData = [];
                SteamLobbyMemberDataCatched[cid] = memberData;
            }

            if (!memberData.TryGetValue(pchKey, out var value))
            {
                value = SteamMatchmaking.Internal.GetLobbyMemberData(lid, cid, pchKey);
                memberData[pchKey] = value;
            }

            return value;
        }

        throw new ArgumentException("GetLobbyMemberData requires SteamIds");
    }

    public void SetLobbyMemberData(ID lobbyId, string pchKey, string pchValue)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
        {
            var localClientId = SteamUser.Internal.GetSteamID();
            if (!SteamLobbyMemberDataCatched.TryGetValue(localClientId, out var memberData))
            {
                memberData = [];
                SteamLobbyMemberDataCatched[localClientId] = memberData;
            }
            memberData[pchKey] = pchValue;
            SteamMatchmaking.Internal.SetLobbyMemberData(id, pchKey, pchValue);
        }
        else
        {
            throw new ArgumentException("SetLobbyMemberData requires a SteamId");
        }
    }

    // ===== Lobby Member Management Methods =====
    public int GetNumLobbyMembers(ID lobbyId)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
            return SteamMatchmaking.Internal.GetNumLobbyMembers(id);
        throw new ArgumentException("GetNumLobbyMembers requires a SteamId");
    }

    public ID GetLobbyMemberByIndex(ID lobbyId, int memberIndex)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
        {
            var member = SteamMatchmaking.Internal.GetLobbyMemberByIndex(id, memberIndex);
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

    internal static ID LobbyOwnerCatched = ID.Null;
    public ID GetLobbyOwner(ID lobbyId)
    {
        if (LobbyOwnerCatched != ID.Null)
        {
            return LobbyOwnerCatched;
        }

        if (lobbyId.TryGetSteamId(out SteamId id))
        {
            var owner = SteamMatchmaking.Internal.GetLobbyOwner(id);
            LobbyOwnerCatched = owner.AsID();
            return LobbyOwnerCatched;
        }
        throw new ArgumentException("GetLobbyOwner requires a SteamId");
    }

    public void Dispose()
    {
    }
}
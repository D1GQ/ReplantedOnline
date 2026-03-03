using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSteamworks;
using Il2CppSteamworks.Data;
using MelonLoader;
using ReplantedOnline.Helper;
using ReplantedOnline.Interfaces;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Patches.Steam;
using ReplantedOnline.Structs;

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
    public bool IsP2PPacketAvailable(out uint msgSize, int channel = 0)
    {
        msgSize = 0;
        return SteamNetworking.Internal.IsP2PPacketAvailable(ref msgSize, channel);
    }

    public bool SendP2PPacket(ID clientId, Il2CppStructArray<byte> data, int length = -1, int nChannel = 0, P2PSend sendType = P2PSend.Reliable)
    {
        if (clientId.TryGetSteamId(out SteamId steamId))
            return SteamNetworking.SendP2PPacket(steamId, data, length, nChannel, sendType);
        throw new ArgumentException("SendP2PPacket requires a SteamId");
    }

    public bool ReadP2PPacket(Il2CppStructArray<byte> buffer, ref uint size, ref ID userId, int channel = 0)
    {
        SteamId steamId = default;
        var result = SteamNetworking.ReadP2PPacket(buffer, ref size, ref steamId, channel);
        if (result)
            userId = steamId.AsID();
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

    public ID GetLobbyMemberByIndex(ID lobbyId, int iMember)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
        {
            var member = SteamMatchmaking.Internal.GetLobbyMemberByIndexOriginal(id, iMember);
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

    public bool SetLobbyMemberLimit(ID lobbyId, int cMaxMembers)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
            return SteamMatchmaking.Internal.SetLobbyMemberLimit(id, cMaxMembers);
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

    public bool SetLobbyJoinable(ID lobbyId, bool bLobbyJoinable)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
            return SteamMatchmaking.Internal.SetLobbyJoinable(id, bLobbyJoinable);
        throw new ArgumentException("SetLobbyJoinable requires a SteamId");
    }

    public bool SetLobbyType(ID lobbyId, LobbyType eLobbyType)
    {
        if (lobbyId.TryGetSteamId(out SteamId id))
            return SteamMatchmaking.Internal.SetLobbyType(id, eLobbyType);
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

    public void OnLobbyCreatedCompleted(Result result, Lobby data)
    {
        if (result == Result.OK)
        {
            NetLobby.LobbyData = new(data.Id, data.Owner.Id);
            NetLobby.LobbyData.InitializeData();
            MelonLogger.Msg($"[NetLobby] Lobby created successfully: {NetLobby.LobbyData.LobbyId}");
            MatchmakingManager.SetLobbyData(NetLobby.LobbyData);
        }
        else
        {
            Transitions.ToMainMenu();
            MelonLogger.Error($"[NetLobby] Lobby creation failed with result: {result}");
        }
    }

    public void OnLobbyEnteredCompleted(Lobby data)
    {
        NetLobby.LobbyData ??= new(data.Id, data.Owner.Id);
        NetLobby.LobbyData.LobbyCode = NetLobby.NetworkTransport.GetLobbyData(NetLobby.LobbyData.LobbyId, ReplantedOnlineMod.Constants.GAME_CODE_KEY);
        Transitions.ToVersus(() =>
        {
            NetworkDispatcher.StartListening();
            NetLobby.LobbyData.UpdateLobbyStates();
            NetClient.LocalClient?.Ready = true;
        });

        NetLobby.ProcessMemberList();

        int memberCount = NetLobby.GetLobbyMemberCount();

        if (memberCount > 1)
        {
            MelonLogger.Msg($"[NetLobby] Joined lobby {NetLobby.LobbyData.LobbyId} with {memberCount} players");
        }
        else
        {
            MelonLogger.Msg($"[NetLobby] Joined lobby {NetLobby.LobbyData.LobbyId} with {memberCount} player");
        }
    }

    public void OnLobbyDataChanged(Lobby lobby)
    {
        if (lobby.Owner.Id != NetLobby.LobbyData?.HostId)
        {
            NetLobby.LeaveLobby(() =>
            {
                ReplantedOnlinePopup.Show("Disconnected", "Host has left the game!");
            });
            MelonLogger.Warning("[NetLobby] Lobby host left the game");
        }
        else
        {
            NetLobby.LobbyData.UpdateLobbyStates();
            NetLobby.ProcessMemberList();
        }
    }

    public void OnLobbyMemberJoined(Lobby lobby, ID clientId)
    {
        if (lobby.Id != NetLobby.LobbyData.LobbyId)
        {
            MelonLogger.Warning($"[NetLobby] Member joined different lobby (ours: {NetLobby.LobbyData.LobbyId}, theirs: {lobby.Id})");
            return;
        }

        MelonLogger.Msg($"[NetLobby] Player {clientId} ({GetMemberName(clientId)}) joined the lobby");
        NetLobby.ProcessMemberList();

        // If we're the host, request P2P session with the new player
        if (NetLobby.AmLobbyHost())
        {
            MelonLogger.Msg($"[NetLobby] Host initiating P2P connection with new player {clientId}");
            NetworkDispatcher.SendNetworkObjectsTo(clientId);
        }
    }

    public void OnLobbyMemberLeave(Lobby lobby, ID user)
    {
        if (NetLobby.AmLobbyHost())
        {
            NetLobby.ResetLobby(() =>
            {
                ReplantedOnlinePopup.Show("Lobby Restarted", "The other player has left the game!");
            });
        }

        NetLobby.ProcessMemberList();
    }

    public void OnP2PSessionRequest(ID clientId)
    {
        if (NetLobby.IsPlayerInOurLobby(clientId))
        {
            if (clientId.IsBanned()) return;

            AcceptP2PSessionWithUser(clientId);
            MelonLogger.Msg($"[NetLobby] Accepted P2P session with {clientId}");
        }
        else
        {
            MelonLogger.Warning($"[NetLobby] Rejected P2P session from non-lobby member: {clientId}");
        }
    }

    public void OnP2PSessionConnectFail(ID clientId, P2PSessionError error)
    {
        MelonLogger.Warning($"[NetLobby] P2P session connection failed with {clientId}: {error}");
    }
}
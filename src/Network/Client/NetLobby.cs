using Il2CppSteamworks;
using Il2CppSteamworks.Data;
using ReplantedOnline.Enums;
using ReplantedOnline.Interfaces;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Modules.Panels;
using ReplantedOnline.Network.Server;
using ReplantedOnline.Network.Server.Packet;
using ReplantedOnline.Network.Server.Transport;
using ReplantedOnline.Patches.Steam;
using ReplantedOnline.Structs;

namespace ReplantedOnline.Network.Client;

/// <summary>
/// Manages Steamworks lobby functionality for ReplantedOnline, handling lobby creation, joining,
/// member management, and P2P connection setup between lobby members.
/// </summary>
internal static class NetLobby
{
    private const int MAX_LOBBY_SIZE = 2;

    /// <summary>
    /// The LobbyData of the current lobby the player is in, or null if not in a lobby.
    /// </summary>
    internal static NetLobbyData LobbyData;

    /// <summary>
    /// Gets the current network transport implementation for lobby and P2P operations.
    /// </summary>
    internal static INetworkTransport NetworkTransport { get; private set; }

    private static int lastTransportMode = -1;

    /// <summary>
    /// Sets the network transport mode based on the provided mode index.
    /// </summary>
    /// <param name="mode"></param>
    internal static void SetTransportMode(int mode)
    {
        if (lastTransportMode == mode) return;

        NetworkTransport?.Dispose();
        NetworkTransport = null;

        switch (mode)
        {
            case 0:
                NetworkTransport = new SteamTransport();
                ReplantedOnlineMod.Logger.Msg("[NetLobby] Network transport set to Steam");
                break;
            case 1:
                NetworkTransport = new LanTransport();
                ReplantedOnlineMod.Logger.Msg("[NetLobby] Network transport set to LAN");
                break;
            default:
                ReplantedOnlineMod.Logger.Warning($"[NetLobby] Invalid transport mode: {mode}, defaulting to Steam");
                NetworkTransport = new SteamTransport();
                break;
        }

        lastTransportMode = mode;
    }

    /// <summary>
    /// Initializes all Steamworks callbacks for lobby and P2P networking events.
    /// </summary>
    internal static void InitializeSteam()
    {
        SteamMatchmaking.OnLobbyCreated += (Action<Result, Lobby>)((result, lobby) =>
        {
            OnLobbyCreatedCompleted(result, new ServerLobby(lobby));
        });

        SteamMatchmaking.OnLobbyEntered += (Action<Lobby>)(lobby =>
        {
            OnLobbyEnteredCompleted(new ServerLobby(lobby));
        });

        SteamMatchmaking.OnLobbyDataChanged += (Action<Lobby>)((lobby) =>
        {
            OnLobbyDataChanged(new ServerLobby(lobby));
        });

        SteamMatchmaking.OnLobbyMemberJoined += (Action<Lobby, Friend>)((lobby, friend) =>
        {
            OnLobbyMemberJoined(new ServerLobby(lobby), friend.Id);
        });

        SteamMatchmaking.OnLobbyMemberLeave += (Action<Lobby, Friend>)((lobby, user) =>
        {
            OnLobbyMemberLeave(new ServerLobby(lobby), user.Id);
        });

        SteamNetworking.OnP2PSessionRequest += (Action<SteamId>)(steamId =>
        {
            OnP2PSessionRequest(steamId);
        });

        SteamNetworking.OnP2PConnectionFailed += (Action<SteamId, P2PSessionError>)((steamId, error) =>
        {
            Steam_OnP2PSessionConnectFail(steamId, error);
        });

        SetTransportMode(BloomEngineManager.BloomConfigs.UseLan.Value ? 1 : 0);

        ReplantedOnlineMod.Logger.Msg("[NetLobby] Steamworks initialized");
    }

    /// <summary>
    /// Resets the lobby state and transitions back to the Versus menu.
    /// </summary>
    internal static void ResetLobby(Action callback = null)
    {
        ReplantedOnlineMod.Logger.Msg("[NetLobby] Restarting the lobby");
        NetClient.LocalClient?.Ready = false;
        VersusLobbyManager.ResetPlayerInput();
        LobbyData.UnsetAllTeams();
        LobbyData.LocalDespawnAll();
        LobbyData.InitializeData();
        Transitions.SetLoading();
        Transitions.ToVersus(() =>
        {
            Transitions.ToGameplay(() =>
            {
                callback?.Invoke();
                NetClient.LocalClient?.Ready = true;
            });
        });
    }

    /// <summary>
    /// Creates a new lobby with a maximum of 2 players (Versus mode).
    /// </summary>
    internal static void CreateLobby()
    {
        NetworkTransport.CreateLobby(MAX_LOBBY_SIZE);
        Transitions.SetLoading();
    }

    /// <summary>
    /// Joins an existing lobby.
    /// </summary>
    internal static void JoinLobby(ID lobbyId)
    {
        NetworkTransport.JoinLobby(lobbyId);
        Transitions.SetLoading();
        ReplantedOnlineMod.Logger.Msg($"[NetLobby] Joining lobby: {lobbyId}");
    }

    /// <summary>
    /// Leaves the current lobby and cleans up network connections.
    /// </summary>
    internal static void LeaveLobby(Action callback = null)
    {
        if (LobbyData == null)
        {
            ReplantedOnlineMod.Logger.Warning("[NetLobby] Cannot leave - not in a lobby");
            return;
        }

        ReplantedOnlineMod.Logger.Msg($"[NetLobby] Leaving lobby {LobbyData.LobbyId}");
        var lobbyId = LobbyData.LobbyId;
        LobbyData.Dispose();
        Transitions.SetLoading();
        Transitions.ToMainMenu(callback);
        LobbyData = null;
        NetworkTransport.LeaveLobby(lobbyId);
        ReplantedOnlineMod.Logger.Msg("[NetLobby] Successfully left lobby");
    }

    internal static void OnLobbyCreatedCompleted(Result result, ServerLobby lobby)
    {
        if (result == Result.OK)
        {
            LobbyData?.Dispose();
            LobbyData = new(lobby.Id, lobby.OwnerId);
            LobbyData.InitializeData();
            ReplantedOnlineMod.Logger.Msg($"[NetLobby] Lobby created successfully: {LobbyData.LobbyId}");
            MatchmakingManager.SetLobbyData(LobbyData);
        }
        else
        {
            Transitions.ToMainMenu();
            ReplantedOnlineMod.Logger.Error($"[NetLobby] Lobby creation failed with result: {result}");
        }
    }

    internal static void OnLobbyEnteredCompleted(ServerLobby lobby)
    {
        LobbyData?.Dispose();
        LobbyData = new(lobby.Id, lobby.OwnerId);
        LobbyData.LobbyCode = NetworkTransport.GetLobbyData(LobbyData.LobbyId, ReplantedOnlineMod.Constants.GAME_CODE_KEY);
        Transitions.ToVersus(() =>
        {
            NetworkDispatcher.StartListening();
            LobbyData.UpdateLobbyStates();
            NetClient.LocalClient?.Ready = true;
        });

        ProcessMemberList();

        int memberCount = GetLobbyMemberCount();

        if (memberCount > 1)
        {
            ReplantedOnlineMod.Logger.Msg($"[NetLobby] Joined lobby {LobbyData.LobbyId} with {memberCount} players");
        }
        else
        {
            ReplantedOnlineMod.Logger.Msg($"[NetLobby] Joined lobby {LobbyData.LobbyId} with {memberCount} player");
        }
    }

    internal static void OnLobbyDataChanged(ServerLobby lobby)
    {
        if (!AmInLobby()) return;

        if (lobby.OwnerId != LobbyData?.HostId)
        {
            LeaveLobby(() =>
            {
                CustomPopupPanel.Show("Disconnected", "Host has left the game!");
            });
            ReplantedOnlineMod.Logger.Warning("[NetLobby] Lobby host left the game");
        }
        else
        {
            LobbyData.UpdateLobbyStates();
        }
    }

    internal static void OnLobbyMemberJoined(ServerLobby lobby, ID clientId)
    {
        if (lobby.Id != LobbyData.LobbyId)
        {
            ReplantedOnlineMod.Logger.Warning($"[NetLobby] Member joined different lobby (ours: {LobbyData.LobbyId}, theirs: {lobby.Id})");
            return;
        }

        ReplantedOnlineMod.Logger.Msg($"[NetLobby] Player {clientId} ({NetworkTransport.GetMemberName(clientId)}) joined the lobby");
        ProcessMemberList();

        // If we're the host, request P2P session with the new player
        if (AmLobbyHost())
        {
            ReplantedOnlineMod.Logger.Msg($"[NetLobby] Host initiating P2P connection with new player {clientId}");
            NetworkDispatcher.SendNetworkObjectsTo(clientId);
        }
    }

    internal static void OnLobbyMemberLeave(ServerLobby lobby, ID clientId)
    {
        if (AmLobbyHost())
        {
            ResetLobby(() =>
            {
                CustomPopupPanel.Show("Lobby Restarted", "The other player has left the game!");
            });
        }

        ProcessMemberList();
    }

    internal static void OnP2PSessionRequest(ID clientId)
    {
        if (clientId.IsBanned()) return;

        NetworkTransport.AcceptP2PSessionWithUser(clientId);
        ReplantedOnlineMod.Logger.Msg($"[NetLobby] Accepted P2P session with {clientId}");
    }

    internal static void Steam_OnP2PSessionConnectFail(ID clientId, P2PSessionError error)
    {
        ReplantedOnlineMod.Logger.Warning($"[NetLobby] P2P session connection failed with {clientId}: {error}");
    }

    /// <summary>
    /// Synchronizes the internal client list with the current lobby members from Steamworks.
    /// Clears the existing client list and repopulates it with current lobby members.
    /// </summary>
    internal static void ProcessMemberList()
    {
        if (AmLobbyHost())
        {
            MatchmakingManager.UpdateLobbyJoinable(false);
            NetworkTransport.SetLobbyMemberLimit(LobbyData.LobbyId, MAX_LOBBY_SIZE);
        }

        List<ID> members = [];
        var num = NetworkTransport.GetNumLobbyMembers(LobbyData.LobbyId);
        for (int i = 0; i < num; i++)
        {
            var member = NetworkTransport.GetLobbyMemberByIndex(LobbyData.LobbyId, i);
            if (!member.IsBanned())
            {
                members.Add(member);
            }
            else
            {
                NetworkTransport.CloseP2PSessionWithUser(member);
            }
        }
        LobbyData.ProcessMembers(members);

        if (AmLobbyHost())
        {
            MatchmakingManager.UpdateLobbyJoinable();
        }
    }

    /// <summary>
    /// Try to remove player from the lobby, if not the P2P will terminate ether way
    /// </summary>
    /// <param name="clientId">The CLient ID of the player to remove.</param>
    /// <param name="reason">The reason for the bam.</param>
    internal static void BanFromLobby(ID clientId, BanReasons reason)
    {
        if (!AmInLobby())
        {
            ReplantedOnlineMod.Logger.Warning("[NetLobby] Cannot kick player - not in a lobby");
            return;
        }

        if (!AmLobbyHost())
        {
            ReplantedOnlineMod.Logger.Warning("[NetLobby] Only the lobby host can kick players");
            return;
        }

        if (clientId == NetworkTransport.LocalClientId)
        {
            ReplantedOnlineMod.Logger.Warning("[NetLobby] Cannot kick yourself");
            return;
        }

        if (!IsPlayerInOurLobby(clientId))
        {
            ReplantedOnlineMod.Logger.Warning($"[NetLobby] Player {clientId} is not in the lobby");
            return;
        }

        var packetWriter = PacketWriter.Get();
        packetWriter.WriteByte((byte)reason);
        NetworkDispatcher.SendPacketTo(clientId, packetWriter, PacketTag.RemoveClient, PacketChannel.Main);
        packetWriter.Recycle();

        clientId.Ban();
    }

    /// <summary>
    /// Gets the number of members currently in the lobby.
    /// </summary>
    /// <returns>The number of lobby members.</returns>
    internal static int GetLobbyMemberCount()
    {
        return LobbyData.AllClients.Count;
    }

    /// <summary>
    /// Gets the Client ID of a lobby member by their index.
    /// </summary>
    /// <param name="index">The zero-based index of the lobby member.</param>
    /// <returns>The Client ID of the lobby member at the specified index.</returns>
    internal static ID GetLobbyMemberByIndex(int index)
    {
        return NetworkTransport.GetLobbyMemberByIndex(LobbyData.LobbyId, index);
    }

    /// <summary>
    /// Gets the Client ID of the lobby owner.
    /// </summary>
    /// <returns>The CLient ID of the lobby owner.</returns>
    internal static ID GetLobbyOwner()
    {
        return NetworkTransport.GetLobbyOwner(LobbyData.LobbyId);
    }

    /// <summary>
    /// Checks if a player is currently in our lobby.
    /// </summary>
    /// <param name="clientId">The Client ID of the player to check.</param>
    /// <returns>True if the player is in our lobby, false otherwise.</returns>
    internal static bool IsPlayerInOurLobby(ID clientId)
    {
        foreach (var client in LobbyData.AllClients.Values)
        {
            if (client.ClientId == clientId)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the player is currently in a lobby.
    /// </summary>
    /// <returns>True if the player is in a lobby, false otherwise.</returns>
    internal static bool AmInLobby() => LobbyData != null;

    /// <summary>
    /// Checks if the local player is the host of the current lobby.
    /// </summary>
    /// <returns>True if the local player is the lobby host, false otherwise.</returns>
    internal static bool AmLobbyHost()
    {
        return GetLobbyOwner() == NetworkTransport.LocalClientId;
    }

    /// <summary>
    /// Checks if a specific player is the host of the current lobby.
    /// </summary>
    /// <param name="id">The Client ID of the player to check.</param>
    /// <returns>True if the specified player is the lobby host, false otherwise.</returns>
    internal static bool AmLobbyHost(ID id)
    {
        return GetLobbyOwner() == id;
    }

    /// <summary>
    /// Gets the game code for the current lobby
    /// </summary>
    internal static string GetCurrentLobbyGameCode()
    {
        if (!AmInLobby()) return string.Empty;
        return NetworkTransport.GetLobbyData(LobbyData.LobbyId, ReplantedOnlineMod.Constants.GAME_CODE_KEY);
    }
}
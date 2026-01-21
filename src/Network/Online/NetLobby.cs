using Il2CppSteamworks;
using Il2CppSteamworks.Data;
using MelonLoader;
using ReplantedOnline.Enums;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Packet;

namespace ReplantedOnline.Network.Online;

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
    /// Initializes all Steamworks callbacks for lobby and P2P networking events.
    /// </summary>
    internal static void Initialize()
    {
        SteamMatchmaking.OnLobbyCreated += (Action<Result, Lobby>)((result, data) =>
        {
            _OnLobbyCreatedCompleted(result, data);
        });

        SteamMatchmaking.OnLobbyEntered += (Action<Lobby>)(data =>
        {
            _OnLobbyEnteredCompleted(data);
        });

        SteamMatchmaking.OnLobbyDataChanged += (Action<Lobby>)((lobby) =>
        {
            _OnLobbyDataChanged(lobby);
        });

        SteamMatchmaking.OnLobbyMemberJoined += (Action<Lobby, Friend>)((lobby, friend) =>
        {
            _OnLobbyMemberJoined(lobby, friend);
        });

        SteamMatchmaking.OnLobbyMemberLeave += (Action<Lobby, Friend>)((data, user) =>
        {
            _OnLobbyMemberLeave(data, user);
        });

        SteamNetworking.OnP2PSessionRequest += (Action<SteamId>)(steamId =>
        {
            _OnP2PSessionRequest(steamId);
        });

        SteamNetworking.OnP2PConnectionFailed += (Action<SteamId, P2PSessionError>)((steamId, error) =>
        {
            _OnP2PSessionConnectFail(steamId, error);
        });


        MelonLogger.Msg("[NetLobby] Steamworks callbacks initialized");
    }

    /// <summary>
    /// Resets the lobby state and transitions back to the Versus menu.
    /// </summary>
    internal static void ResetLobby(Action callback = null)
    {
        MelonLogger.Msg("[NetLobby] Restarting the lobby");
        VersusLobbyManager.ResetPlayerInput();
        LobbyData.UnsetAllClientsReady();
        LobbyData.UnsetAllTeams();
        LobbyData.LocalDespawnAll();
        LobbyData.Networked = new();
        Transitions.SetLoading();
        Transitions.ToVersus();
        Transitions.ToGameplay(() =>
        {
            if (AmLobbyHost())
            {
                LobbyData.Networked.SendAllData();
            }

            callback?.Invoke();
        });

        if (AmLobbyHost())
        {
            MatchmakingManager.SetJoinable(true);
        }
    }

    /// <summary>
    /// Creates a new lobby with a maximum of 2 players (Versus mode).
    /// </summary>
    internal static void CreateLobby()
    {
        SteamMatchmaking.CreateLobbyAsync(MAX_LOBBY_SIZE);
        Transitions.SetLoading();
    }

    /// <summary>
    /// Joins an existing lobby by its Steam ID.
    /// </summary>
    internal static void JoinLobby(SteamId lobbyId)
    {
        SteamMatchmaking.JoinLobbyAsync(lobbyId);
        Transitions.SetLoading();
        MelonLogger.Msg($"[NetLobby] Joining lobby: {lobbyId}");
    }

    /// <summary>
    /// Leaves the current lobby and cleans up network connections.
    /// </summary>
    internal static void LeaveLobby(Action callback = null)
    {
        if (LobbyData == null)
        {
            MelonLogger.Warning("[NetLobby] Cannot leave - not in a lobby");
            return;
        }

        MelonLogger.Msg($"[NetLobby] Leaving lobby {LobbyData.LobbyId}");
        SteamMatchmaking.Internal.LeaveLobby(LobbyData.LobbyId);
        LobbyData.LocalDespawnAll();
        Transitions.ToMainMenu(callback);
        LobbyData = null;
        MelonLogger.Msg("[NetLobby] Successfully left lobby");
    }

    /// <summary>
    /// Callback handler for when a lobby creation request completes.
    /// </summary>
    /// <param name="result">The result of the lobby creation attempt.</param>
    /// <param name="data">The lobby data if creation was successful.</param>
    private static void _OnLobbyCreatedCompleted(Result result, Lobby data)
    {
        if (result == Result.OK)
        {
            LobbyData = new(data.Id, data.Owner.Id);
            MelonLogger.Msg($"[NetLobby] Lobby created successfully: {LobbyData.LobbyId}");
            MatchmakingManager.SetLobbyData(LobbyData);
        }
        else
        {
            Transitions.ToMainMenu();
            MelonLogger.Error($"[NetLobby] Lobby creation failed with result: {result}");
        }
    }

    /// <summary>
    /// Callback handler for when a player successfully enters a lobby.
    /// </summary>
    /// <param name="data">The lobby data that was entered.</param>
    private static void _OnLobbyEnteredCompleted(Lobby data)
    {
        LobbyData ??= new(data.Id, data.Owner.Id);
        LobbyData.LobbyCode = SteamMatchmaking.Internal.GetLobbyData(LobbyData.LobbyId, ReplantedOnlineMod.Constants.GAME_CODE_KEY);
        Transitions.ToVersus(NetworkDispatcher.StartListening);

        ProcessMemberList();

        int memberCount = GetLobbyMemberCount();

        if (memberCount > 1)
        {
            MelonLogger.Msg($"[NetLobby] Joined lobby {LobbyData.LobbyId} with {memberCount} players");
        }
        else
        {
            MelonLogger.Msg($"[NetLobby] Joined lobby {LobbyData.LobbyId} with {memberCount} player");
        }
    }

    /// <summary>
    /// Callback handler for when a lobby's data changes.
    /// </summary>
    /// <param name="lobby">The lobby dara.</param>
    private static void _OnLobbyDataChanged(Lobby lobby)
    {
        if (lobby.Owner.Id != LobbyData?.HostId)
        {
            LeaveLobby(() =>
            {
                ReplantedOnlinePopup.Show("Disconnected", "Host has left the game!");
            });
            MelonLogger.Warning("[NetLobby] Lobby host left the game");
        }
    }

    /// <summary>
    /// Callback handler for when a new member joins the lobby.
    /// </summary>
    /// <param name="lobby">The lobby that was joined.</param>
    /// <param name="user">The friend who joined the lobby.</param>
    private static void _OnLobbyMemberJoined(Lobby lobby, Friend user)
    {
        if (lobby.Id != LobbyData.LobbyId)
        {
            MelonLogger.Warning($"[NetLobby] Member joined different lobby (ours: {LobbyData.LobbyId}, theirs: {lobby.Id})");
            return;
        }

        SteamId joinedPlayerId = user.Id;
        MelonLogger.Msg($"[NetLobby] Player {joinedPlayerId} ({user.Name}) joined the lobby");
        ProcessMemberList();

        // If we're the host, request P2P session with the new player
        if (AmLobbyHost())
        {
            MelonLogger.Msg($"[NetLobby] Host initiating P2P connection with new player {joinedPlayerId}");
            NetworkDispatcher.SendNetworkObjectsTo(joinedPlayerId);
            LobbyData.Networked.SendAllData();
        }
    }

    /// <summary>
    /// Callback handler for when a member leaves the lobby.
    /// </summary>
    /// <param name="lobby">The lobby that was left.</param>
    /// <param name="user">The friend who left the lobby.</param>
    private static void _OnLobbyMemberLeave(Lobby lobby, Friend user)
    {
        if (AmLobbyHost())
        {
            ResetLobby(() =>
            {
                ReplantedOnlinePopup.Show("Lobby Restarted", "The other player has left the game!");
            });
        }

        ProcessMemberList();
    }

    /// <summary>
    /// Callback handler for when a P2P session request is received from another player.
    /// </summary>
    /// <param name="steamId">The Steam ID of the player requesting the session.</param>
    private static void _OnP2PSessionRequest(SteamId steamId)
    {
        if (IsPlayerInOurLobby(steamId))
        {
            SteamNetworking.AcceptP2PSessionWithUser(steamId);
            MelonLogger.Msg($"[NetLobby] Accepted P2P session with {steamId}");
        }
        else
        {
            MelonLogger.Warning($"[NetLobby] Rejected P2P session from non-lobby member: {steamId}");
        }
    }

    /// <summary>
    /// Callback handler for when a P2P session connection fails.
    /// </summary>
    /// <param name="steamId">The Steam ID of the player the connection failed with.</param>
    /// <param name="error">The error that occurred during connection.</param>
    private static void _OnP2PSessionConnectFail(SteamId steamId, P2PSessionError error)
    {
        MelonLogger.Warning($"[NetLobby] P2P session connection failed with {steamId}: {error}");
    }

    /// <summary>
    /// Synchronizes the internal client list with the current lobby members from Steamworks.
    /// Clears the existing client list and repopulates it with current lobby members.
    /// </summary>
    internal static void ProcessMemberList()
    {
        List<SteamId> members = [];
        var num = SteamMatchmaking.Internal.GetNumLobbyMembers(LobbyData.LobbyId);
        for (int i = 0; i < num; i++)
        {
            var member = SteamMatchmaking.Internal.GetLobbyMemberByIndex(LobbyData.LobbyId, i);
            members.Add(member);
        }
        LobbyData.ProcessMembers(members);
    }

    /// <summary>
    /// Try to remove player from the lobby, if not the P2P will terminate ether way
    /// </summary>
    /// <param name="steamId">The Steam ID of the player to remove.</param>
    /// <param name="reason">The reason for the bam.</param>
    internal static void TryRemoveFromLobby(SteamId steamId, BanReasons reason)
    {
        if (!AmInLobby())
        {
            MelonLogger.Warning("[NetLobby] Cannot kick player - not in a lobby");
            return;
        }

        if (!AmLobbyHost())
        {
            MelonLogger.Warning("[NetLobby] Only the lobby host can kick players");
            return;
        }

        if (steamId == SteamUser.Internal.GetSteamID())
        {
            MelonLogger.Warning("[NetLobby] Cannot kick yourself");
            return;
        }

        if (!IsPlayerInOurLobby(steamId))
        {
            MelonLogger.Warning($"[NetLobby] Player {steamId} is not in the lobby");
            return;
        }

        var packetWriter = PacketWriter.Get();
        packetWriter.WriteByte((byte)reason);
        NetworkDispatcher.SendPacketTo(steamId, packetWriter, PacketTag.RemoveClient, PacketChannel.Main);
        packetWriter.Recycle();

        TerminateP2PSession(steamId);
    }

    /// <summary>
    /// Terminates all P2P sessions with a player, preventing network communication.
    /// </summary>
    /// <param name="steamId">The Steam ID of the player to disconnect from.</param>
    private static void TerminateP2PSession(SteamId steamId)
    {
        try
        {
            bool sessionClosed = SteamNetworking.CloseP2PSessionWithUser(steamId);

            if (sessionClosed)
            {
                MelonLogger.Msg($"[NetLobby] P2P session terminated with {steamId}");
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[NetLobby] Error terminating P2P session with {steamId}: {ex.Message}");
        }
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
    /// Gets the Steam ID of a lobby member by their index.
    /// </summary>
    /// <param name="index">The zero-based index of the lobby member.</param>
    /// <returns>The Steam ID of the lobby member at the specified index.</returns>
    internal static SteamId GetLobbyMemberByIndex(int index)
    {
        return SteamMatchmaking.Internal.GetLobbyMemberByIndex(LobbyData.LobbyId, index);
    }

    /// <summary>
    /// Gets the Steam ID of the lobby owner.
    /// </summary>
    /// <returns>The Steam ID of the lobby owner.</returns>
    internal static SteamId GetLobbyOwner()
    {
        return SteamMatchmaking.Internal.GetLobbyOwner(LobbyData.LobbyId);
    }

    /// <summary>
    /// Checks if a player is currently in our lobby.
    /// </summary>
    /// <param name="steamId">The Steam ID of the player to check.</param>
    /// <returns>True if the player is in our lobby, false otherwise.</returns>
    internal static bool IsPlayerInOurLobby(SteamId steamId)
    {
        foreach (var client in LobbyData.AllClients.Values)
        {
            if (client.SteamId == steamId)
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
        return GetLobbyOwner() == SteamUser.Internal.GetSteamID();
    }

    /// <summary>
    /// Checks if a specific player is the host of the current lobby.
    /// </summary>
    /// <param name="id">The Steam ID of the player to check.</param>
    /// <returns>True if the specified player is the lobby host, false otherwise.</returns>
    internal static bool AmLobbyHost(SteamId id)
    {
        return GetLobbyOwner() == id;
    }

    /// <summary>
    /// Gets the game code for the current lobby
    /// </summary>
    internal static string GetCurrentLobbyGameCode()
    {
        if (!AmInLobby()) return string.Empty;
        return SteamMatchmaking.Internal.GetLobbyData(LobbyData.LobbyId, ReplantedOnlineMod.Constants.GAME_CODE_KEY);
    }
}
using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Helper;
using ReplantedOnline.Items.Enums;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.RPC.Handlers;

namespace ReplantedOnline.Network.Online;

/// <summary>
/// Manages Steamworks lobby functionality for ReplantedOnline, handling lobby creation, joining,
/// member management, and P2P connection setup between lobby members.
/// </summary>
internal static class NetLobby
{
    /// <summary>
    /// The LobbyData of the current lobby the player is in, or default if not in a lobby.
    /// </summary>
    internal static NetLobbyData LobbyData = default;

    private const int MAX_LOBBY_SIZE = 2;

    /// <summary>
    /// Initializes all Steamworks callbacks for lobby and P2P networking events.
    /// </summary>
    internal static void Initialize()
    {
        SteamMatchmaking.OnLobbyCreated += (Action<Result, Il2CppSteamworks.Data.Lobby>)((result, data) =>
        {
            OnLobbyCreatedCompleted(result, data);
        });

        SteamMatchmaking.OnLobbyEntered += (Action<Il2CppSteamworks.Data.Lobby>)(data =>
        {
            OnLobbyEnteredCompleted(data);
        });

        SteamMatchmaking.OnLobbyMemberJoined += (Action<Il2CppSteamworks.Data.Lobby, Friend>)((lobby, friend) =>
        {
            OnLobbyMemberJoined(lobby, friend);
        });

        SteamMatchmaking.OnLobbyMemberLeave += (Action<Il2CppSteamworks.Data.Lobby, Friend>)((data, user) =>
        {
            OnLobbyMemberLeave(data, user);
        });

        SteamNetworking.OnP2PSessionRequest += (Action<SteamId>)(steamId =>
        {
            OnP2PSessionRequest(steamId);
        });

        SteamNetworking.OnP2PConnectionFailed += (Action<SteamId, P2PSessionError>)((steamId, error) =>
        {
            OnP2PSessionConnectFail(steamId, error);
        });

        MelonLogger.Msg("[NetLobby] Steamworks callbacks initialized");
    }

    /// <summary>
    /// Creates a new lobby with a maximum of 2 players (Versus mode).
    /// </summary>
    internal static void CreateLobby()
    {
        SteamMatchmaking.CreateLobbyAsync(MAX_LOBBY_SIZE);
        MelonLogger.Msg("[NetLobby] Creating lobby...");
    }

    /// <summary>
    /// Joins an existing lobby by its Steam ID.
    /// </summary>
    internal static void JoinLobby(SteamId lobbyId)
    {
        SteamMatchmaking.JoinLobbyAsync(lobbyId);
        MelonLogger.Msg($"[NetLobby] Joining lobby: {lobbyId}");
    }

    /// <summary>
    /// Leaves the current lobby and cleans up network connections.
    /// </summary>
    internal static void LeaveLobby()
    {
        if (LobbyData == default)
        {
            MelonLogger.Warning("[NetLobby] Cannot leave - not in a lobby");
            return;
        }

        MelonLogger.Msg($"[NetLobby] Leaving lobby {LobbyData.LobbyId}");
        SteamMatchmaking.Internal.LeaveLobby(LobbyData.LobbyId);
        Transitions.ToMainMenu();
        LobbyData = default;
        MelonLogger.Msg("[NetLobby] Successfully left lobby");
    }

    /// <summary>
    /// Retrieves lobby data for the specified key.
    /// </summary>
    /// <param name="key">The key of the lobby data to retrieve.</param>
    /// <returns>The lobby data value, or empty string if not found.</returns>
    internal static string GetLobbyData(string key)
    {
        return SteamMatchmaking.Internal.GetLobbyData(LobbyData.LobbyId, key);
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
    /// Callback handler for when a lobby creation request completes.
    /// </summary>
    /// <param name="result">The result of the lobby creation attempt.</param>
    /// <param name="data">The lobby data if creation was successful.</param>
    private static void OnLobbyCreatedCompleted(Result result, Il2CppSteamworks.Data.Lobby data)
    {
        if (result == Result.OK)
        {
            LobbyData = new(data.Id);
            MelonLogger.Msg($"[NetLobby] Lobby created successfully: {LobbyData.LobbyId}");

            SteamMatchmaking.Internal.SetLobbyData(LobbyData.LobbyId, "mod_version", ModInfo.ModVersion);
            SteamMatchmaking.Internal.SetLobbyType(LobbyData.LobbyId, LobbyType.FriendsOnly);
        }
        else
        {
            MelonLogger.Error($"[NetLobby] Lobby creation failed with result: {result}");
        }
    }

    /// <summary>
    /// Callback handler for when a player successfully enters a lobby.
    /// </summary>
    /// <param name="data">The lobby data that was entered.</param>
    private static void OnLobbyEnteredCompleted(Il2CppSteamworks.Data.Lobby data)
    {
        if (LobbyData == default)
        {
            LobbyData = new(data.Id);
        }

        TryProcessMembers();

        int memberCount = GetLobbyMemberCount();

        Transitions.ToVersus();

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
    /// Callback handler for when a new member joins the lobby.
    /// </summary>
    /// <param name="lobby">The lobby that was joined.</param>
    /// <param name="user">The friend who joined the lobby.</param>
    private static void OnLobbyMemberJoined(Il2CppSteamworks.Data.Lobby lobby, Friend user)
    {
        if (lobby.Id != LobbyData.LobbyId)
        {
            MelonLogger.Warning($"[NetLobby] Member joined different lobby (ours: {LobbyData.LobbyId}, theirs: {lobby.Id})");
            return;
        }

        SteamId joinedPlayerId = user.Id;
        MelonLogger.Msg($"[NetLobby] Player {joinedPlayerId} ({user.Name}) joined the lobby");

        // If we're the host, request P2P session with the new player
        if (AmLobbyHost())
        {
            MelonLogger.Msg($"[NetLobby] Host initiating P2P connection with new player {joinedPlayerId}");
            RequestP2PSessionWithPlayer(joinedPlayerId);
        }
    }

    /// <summary>
    /// Callback handler for when a member leaves the lobby.
    /// </summary>
    /// <param name="lobby">The lobby that was left.</param>
    /// <param name="user">The friend who left the lobby.</param>
    private static void OnLobbyMemberLeave(Il2CppSteamworks.Data.Lobby lobby, Friend user)
    {
        TryProcessMembers();
    }

    /// <summary>
    /// Callback handler for when a P2P session request is received from another player.
    /// </summary>
    /// <param name="steamId">The Steam ID of the player requesting the session.</param>
    private static void OnP2PSessionRequest(SteamId steamId)
    {
        if (IsPlayerInOurLobby(steamId))
        {
            SteamNetworking.AcceptP2PSessionWithUser(steamId);
            steamId.GetNetClient()?.HasEstablishedP2P = true;
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
    private static void OnP2PSessionConnectFail(SteamId steamId, P2PSessionError error)
    {
        MelonLogger.Warning($"[NetLobby] P2P session connection failed with {steamId}: {error}");

        if (IsPlayerInOurLobby(steamId) && AmLobbyHost())
        {
            MelonLogger.Msg($"[NetLobby] Retrying P2P connection with {steamId}");
            RequestP2PSessionWithPlayer(steamId);
        }
    }

    /// <summary>
    /// Synchronizes the internal client list with the current lobby members from Steamworks.
    /// Clears the existing client list and repopulates it with current lobby members.
    /// </summary>
    internal static void TryProcessMembers()
    {
        LobbyData.AllClients.Clear();
        List<SteamId> members = [];
        var num = SteamMatchmaking.Internal.GetNumLobbyMembers(LobbyData.LobbyId);
        for (int i = 0; i < num; i++)
        {
            var member = SteamMatchmaking.Internal.GetLobbyMemberByIndex(LobbyData.LobbyId, i);
            members.Add(member);
        }
        LobbyData.ProcessMembers(members);

        if (AmLobbyHost())
        {
            SetupP2PWithLobbyMembers();
        }
    }

    /// <summary>
    /// Establishes P2P connections with all current lobby members.
    /// </summary>
    private static void SetupP2PWithLobbyMembers()
    {
        foreach (var client in LobbyData.AllClients.Values)
        {
            if (client.AmLocal || client.HasEstablishedP2P) continue;

            MelonLogger.Msg($"[NetLobby] Requesting P2P session with {client.Name} as host");
            RequestP2PSessionWithPlayer(client.SteamId);
        }
    }

    /// <summary>
    /// Requests a P2P session with a specific player by sending a dummy packet.
    /// </summary>
    /// <param name="steamId">The Steam ID of the player to connect with.</param>
    private static void RequestP2PSessionWithPlayer(SteamId steamId)
    {
        try
        {
            if (LobbyData?.Banned.Contains(steamId) == true)
            {
                MelonLogger.Msg($"[NetLobby] Skipping P2P request to banned player: {steamId}");
                TryRemoveFromLobby(steamId, BanReasons.Banned);
                return;
            }

            // Send a small dummy packet to initiate P2P connection
            // This will trigger the remote client's OnP2PSessionRequest
            var packetWriter = PacketWriter.Get();
            packetWriter.AddTag(PacketTag.P2P);
            var sent = SteamNetworking.SendP2PPacket(steamId, packetWriter.GetBytes(), packetWriter.Length);
            packetWriter.Recycle();

            UpdateGameStateHandler.Send(LobbyData.LastGameState, false);

            if (sent)
            {
                MelonLogger.Msg($"[NetLobby] Successfully requested P2P session with {steamId}");
            }
            else
            {
                MelonLogger.Warning($"[NetLobby] Failed to request P2P session with {steamId}");
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[NetLobby] Error requesting P2P session with {steamId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Kicks a player from the current lobby and terminates P2P connections.
    /// </summary>
    /// <param name="steamId">The Steam ID of the player to kick.</param>
    /// <param name="reason">The reason for the bam.</param>
    internal static void BanPlayer(SteamId steamId, BanReasons reason = BanReasons.ByHost)
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

        try
        {
            LobbyData.Banned.Add(steamId);

            TryRemoveFromLobby(steamId, reason);

            MelonLogger.Msg($"[NetLobby] Kicked and banned player {steamId} (P2P terminated)");
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[NetLobby] Error kicking player {steamId}: {ex.Message}");
        }
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
        NetworkDispatcher.SendTo(steamId, packetWriter, PacketTag.P2PClose);

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
    internal static bool AmInLobby() => LobbyData != default;

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
}
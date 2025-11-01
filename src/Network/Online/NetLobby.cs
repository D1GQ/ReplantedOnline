using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Items.Enums;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Packet;

namespace ReplantedOnline.Network.Online;

/// <summary>
/// Manages Steamworks lobby functionality for ReplantedOnline, handling lobby creation, joining,
/// member management, and P2P connection setup between lobby members.
/// </summary>
internal class NetLobby
{
    /// <summary>
    /// The Steam ID of the current lobby the player is in, or default if not in a lobby.
    /// </summary>
    internal static SteamId CurrentLobby = default;

    private static readonly HashSet<SteamId> _connectedMembers = [];

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
    }

    /// <summary>
    /// Creates a new lobby with a maximum of 2 players (Versus mode).
    /// </summary>
    internal static void CreateLobby()
    {
        SteamMatchmaking.CreateLobbyAsync(2);
        MelonLogger.Msg("Creating lobby...");
    }

    /// <summary>
    /// Joins an existing lobby by its Steam ID.
    /// </summary>
    internal static void JoinLobby(SteamId lobbyId)
    {
        SteamMatchmaking.JoinLobbyAsync(lobbyId);
        MelonLogger.Msg($"Joining lobby: {lobbyId}");
    }

    /// <summary>
    /// Leaves the current lobby and cleans up network connections.
    /// </summary>
    internal static void LeaveLobby()
    {
        SteamMatchmaking.Internal.LeaveLobby(CurrentLobby);
        SteamNetClient.Clear();
        CurrentLobby = default;
        _connectedMembers.Clear();
        Transitions.ToMainMenu();
        MelonLogger.Msg("Left lobby");
    }

    /// <summary>
    /// Retrieves lobby data for the specified key.
    /// </summary>
    /// <param name="key">The key of the lobby data to retrieve.</param>
    /// <returns>The lobby data value, or empty string if not found.</returns>
    internal static string GetLobbyData(string key)
    {
        return SteamMatchmaking.Internal.GetLobbyData(CurrentLobby, key);
    }

    /// <summary>
    /// Gets the number of members currently in the lobby.
    /// </summary>
    /// <returns>The number of lobby members.</returns>
    internal static int GetLobbyMemberCount()
    {
        return SteamMatchmaking.Internal.GetNumLobbyMembers(CurrentLobby);
    }

    /// <summary>
    /// Gets the Steam ID of a lobby member by their index.
    /// </summary>
    /// <param name="index">The zero-based index of the lobby member.</param>
    /// <returns>The Steam ID of the lobby member at the specified index.</returns>
    internal static SteamId GetLobbyMemberByIndex(int index)
    {
        return SteamMatchmaking.Internal.GetLobbyMemberByIndex(CurrentLobby, index);
    }

    /// <summary>
    /// Gets the Steam ID of the lobby owner.
    /// </summary>
    /// <returns>The Steam ID of the lobby owner.</returns>
    internal static SteamId GetLobbyOwner()
    {
        return SteamMatchmaking.Internal.GetLobbyOwner(CurrentLobby);
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
            CurrentLobby = data.Id;
            MelonLogger.Msg($"Lobby created successfully: {CurrentLobby}");

            SteamMatchmaking.Internal.SetLobbyData(CurrentLobby, "mod_version", ModInfo.Version);
            SteamMatchmaking.Internal.SetLobbyData(CurrentLobby, "game", "ReplantedOnline");
            SteamMatchmaking.Internal.SetLobbyType(CurrentLobby, LobbyType.FriendsOnly);

            // Clear connected members when creating a new lobby
            _connectedMembers.Clear();
            _connectedMembers.Add(SteamUser.Internal.GetSteamID());
            AddAllNetClients();
        }
        else
        {
            MelonLogger.Error($"Lobby creation failed");
        }
    }

    /// <summary>
    /// Callback handler for when a player successfully enters a lobby.
    /// </summary>
    /// <param name="data">The lobby data that was entered.</param>
    private static void OnLobbyEnteredCompleted(Il2CppSteamworks.Data.Lobby data)
    {
        CurrentLobby = data.Id;
        int memberCount = GetLobbyMemberCount();

        Transitions.ToVersus();
        MelonLogger.Msg($"Joined lobby with {memberCount} players");

        // Clear and rebuild connected members
        _connectedMembers.Clear();
        _connectedMembers.Add(SteamUser.Internal.GetSteamID());
        AddAllNetClients();

        SetupP2PWithLobbyMembers();
    }

    /// <summary>
    /// Callback handler for when a new member joins the lobby.
    /// </summary>
    /// <param name="lobby">The lobby that was joined.</param>
    /// <param name="user">The friend who joined the lobby.</param>
    private static void OnLobbyMemberJoined(Il2CppSteamworks.Data.Lobby lobby, Friend user)
    {
        if (lobby.Id != CurrentLobby) return;

        SteamNetClient.Add(user.Id);
        SteamId joinedPlayerId = user.Id;
        MelonLogger.Msg($"Player {joinedPlayerId} joined the lobby");

        // If we're the host, request P2P session with the new player
        if (IsLobbyHost())
        {
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
        SteamNetClient.Remove(user.Id);
        _connectedMembers.Remove(user.Id);
        MelonLogger.Msg($"Player {user.Id} left the lobby");
    }

    /// <summary>
    /// Establishes P2P connections with all current lobby members.
    /// </summary>
    private static void SetupP2PWithLobbyMembers()
    {
        int memberCount = GetLobbyMemberCount();
        for (int i = 0; i < memberCount; i++)
        {
            SteamId member = GetLobbyMemberByIndex(i);
            if (member != SteamUser.Internal.GetSteamID())
            {
                if (IsLobbyHost())
                {
                    // Host initiates P2P connection to all existing members
                    RequestP2PSessionWithPlayer(member);
                }
                _connectedMembers.Add(member);
            }
        }
    }

    /// <summary>
    /// Requests a P2P session with a specific player by sending a dummy packet.
    /// </summary>
    /// <param name="steamId">The Steam ID of the player to connect with.</param>
    private static void RequestP2PSessionWithPlayer(SteamId steamId)
    {
        if (_connectedMembers.Contains(steamId))
        {
            MelonLogger.Msg($"P2P session already established with {steamId}");
            return;
        }

        try
        {
            // Send a small dummy packet to initiate P2P connection
            // This will trigger the remote client's OnP2PSessionRequest
            var packetWriter = PacketWriter.Get();
            packetWriter.AddTag(PacketTag.P2P);
            bool sent = SteamNetworking.SendP2PPacket(steamId, packetWriter.GetBytes(), packetWriter.Length);
            packetWriter.Recycle();

            if (sent)
            {
                MelonLogger.Msg($"Requested P2P session with {steamId}");
            }
            else
            {
                MelonLogger.Warning($"Failed to request P2P session with {steamId}");
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"Error requesting P2P session with {steamId}: {ex.Message}");
        }
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
            _connectedMembers.Add(steamId);
            MelonLogger.Msg($"Accepted P2P session with {steamId}");
        }
        else
        {
            MelonLogger.Warning($"Rejected P2P session from non-lobby member: {steamId}");
        }
    }

    /// <summary>
    /// Callback handler for when a P2P session connection fails.
    /// </summary>
    /// <param name="steamId">The Steam ID of the player the connection failed with.</param>
    /// <param name="error">The error that occurred during connection.</param>
    private static void OnP2PSessionConnectFail(SteamId steamId, P2PSessionError error)
    {
        MelonLogger.Warning($"P2P session connection failed with {steamId}: {error}");
        _connectedMembers.Remove(steamId);

        if (IsPlayerInOurLobby(steamId) && IsLobbyHost())
        {
            MelonLogger.Msg($"Retrying P2P connection with {steamId}");
        }
    }

    /// <summary>
    /// Checks if a player is currently in our lobby.
    /// </summary>
    /// <param name="steamId">The Steam ID of the player to check.</param>
    /// <returns>True if the player is in our lobby, false otherwise.</returns>
    internal static bool IsPlayerInOurLobby(SteamId steamId)
    {
        int memberCount = GetLobbyMemberCount();
        for (int i = 0; i < memberCount; i++)
        {
            if (GetLobbyMemberByIndex(i) == steamId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if the player is currently in a lobby.
    /// </summary>
    /// <returns>True if the player is in a lobby, false otherwise.</returns>
    internal static bool IsInLobby() => CurrentLobby != default;

    /// <summary>
    /// Checks if the local player is the host of the current lobby.
    /// </summary>
    /// <returns>True if the local player is the lobby host, false otherwise.</returns>
    internal static bool IsLobbyHost()
    {
        return GetLobbyOwner() == SteamUser.Internal.GetSteamID();
    }

    /// <summary>
    /// Checks if a specific player is the host of the current lobby.
    /// </summary>
    /// <param name="id">The Steam ID of the player to check.</param>
    /// <returns>True if the specified player is the lobby host, false otherwise.</returns>
    internal static bool IsLobbyHost(SteamId id)
    {
        return GetLobbyOwner() == id;
    }

    /// <summary>
    /// Adds all current lobby members to the network client list.
    /// </summary>
    internal static void AddAllNetClients()
    {
        var num = SteamMatchmaking.Internal.GetNumLobbyMembers(CurrentLobby);
        for (int i = 0; i < num; i++)
        {
            var member = SteamMatchmaking.Internal.GetLobbyMemberByIndex(CurrentLobby, i);
            SteamNetClient.Add(member);
        }
    }
}
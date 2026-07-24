using Il2CppSteamworks;
using Il2CppSteamworks.Data;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Managers.Modded;
using ReplantedOnline.Managers.Reloaded;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Modules.Reloaded.Panel;
using ReplantedOnline.Network.Discord;
using ReplantedOnline.Network.Reloaded.Client.Routing;
using ReplantedOnline.Network.Reloaded.Client.Routing.Packet;
using ReplantedOnline.Network.Reloaded.Client.Routing.Transport;
using ReplantedOnline.Network.Reloaded.Server.Lan;
using ReplantedOnline.Patches.Steam;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.MelonLoader;

namespace ReplantedOnline.Network.Reloaded.Client;

/// <summary>
/// Manages Steamworks lobby functionality for ReplantedOnline, handling lobby creation, joining,
/// member management, and P2P connection setup between lobby members.
/// </summary>
internal static class ReloadedLobby
{
    internal const int MAX_LOBBY_SIZE = 2;

    /// <summary>
    /// The LobbyData of the current lobby the player is in, or null if not in a lobby.
    /// </summary>
    internal static ReloadedLobbyData? LobbyData;

    /// <summary>
    /// Gets the current network transport implementation for lobby and P2P operations.
    /// </summary>
    internal static INetworkTransport? NetworkTransport { get; private set; }

    /// <summary>
    /// Gets the current transport mode being used for network communication.
    /// </summary>
    internal static TransportMode TransportMode { get; private set; } = (TransportMode)int.MinValue;

    /// <summary>
    /// Sets the network transport mode based on the provided mode.
    /// </summary>
    /// <param name="mode">The transport mode to set.</param>
    internal static void SetTransportMode(TransportMode mode)
    {
        if (TransportMode == mode) return;

        NetworkTransport?.Dispose();
        NetworkTransport = null;

        switch (mode)
        {
            case TransportMode.Steam:
                NetworkTransport = new SteamTransport();
                ReplantedOnlineMod.Logger.Msg(typeof(ReloadedLobby), "Network transport set to Steam");
                break;
            case TransportMode.Lan:
                NetworkTransport = new LanTransport();
                ReplantedOnlineMod.Logger.Msg(typeof(ReloadedLobby), "Network transport set to LAN");
                break;
            default:
                ReplantedOnlineMod.Logger.Warning(typeof(ReloadedLobby), $"Invalid transport mode: {mode}, defaulting to Steam");
                NetworkTransport = new SteamTransport();
                break;
        }

        TransportMode = mode;
    }

    /// <summary>
    /// Initializes ReplantedLobby callbacks.
    /// </summary>
    internal static void Initialize()
    {
        InitializeSteam();
        InitializeLan();
        SetTransportMode(BloomEngineManager.BloomConfigs.TransportModeConfig.Value);
        NetworkManager.Heartbeat.OnClientHeartbeatTimeout += client =>
        {
            OnLobbyMemberLeave(LobbyData!.ServerLobby, client.ClientId);
        };

        ReplantedOnlineMod.Logger.Msg(typeof(ReloadedLobby), "Initialized");
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
            SteamTransport.LobbyOwnerCached = ID.Null;
            SteamTransport.SteamLobbyDataCached.Clear();
            SteamTransport.SteamLobbyMemberDataCached.Clear();
            OnLobbyEnteredCompleted(new ServerLobby(lobby));
        });

        SteamMatchmaking.OnLobbyDataChanged += (Action<Lobby>)((lobby) =>
        {
            SteamTransport.SteamLobbyDataCached.Clear();
            OnLobbyDataChanged(new ServerLobby(lobby));
        });

        SteamMatchmaking.OnLobbyMemberDataChanged += (Action<Lobby, Friend>)((lobby, friend) =>
        {
            SteamTransport.SteamLobbyMemberDataCached.Clear();
        });

        SteamMatchmaking.OnLobbyMemberJoined += (Action<Lobby, Friend>)((lobby, friend) =>
        {
            SteamTransport.LobbyOwnerCached = ID.Null;
            OnLobbyMemberJoined(new ServerLobby(lobby), friend.Id);
        });

        SteamMatchmaking.OnLobbyMemberLeave += (Action<Lobby, Friend>)((lobby, user) =>
        {
            SteamTransport.LobbyOwnerCached = ID.Null;
            OnLobbyMemberLeave(new ServerLobby(lobby), user.Id);
        });

        SteamNetworking.OnP2PSessionRequest += (Action<SteamId>)(steamId =>
        {
            OnP2PSessionRequest(steamId);
        });
    }

    /// <summary>
    /// Initializes all Lan callbacks for lobby and P2P networking events.
    /// </summary>
    internal static void InitializeLan()
    {
        LanServer.OnLobbyCreatedCompleted += OnLobbyCreatedCompleted;

        LanServer.OnLobbyEnteredCompleted += OnLobbyEnteredCompleted;

        LanServer.OnLobbyDataChanged += OnLobbyDataChanged;

        LanServer.OnLobbyMemberJoined += OnLobbyMemberJoined;

        LanServer.OnLobbyMemberLeave += OnLobbyMemberLeave;
    }

    /// <summary>
    /// Resets the lobby state and transitions back to the Versus menu.
    /// </summary>
    internal static void ResetLobby(Action? callback = null)
    {
        ReplantedOnlineMod.Logger.Msg(typeof(ReloadedLobby), "Restarting the lobby");
        ReloadedClientData.LocalClient?.Ready.Value = false;
        DiscordManager.OnLobbyRestart();
        InputManager.ResetPlayerInput();
        InputManager.SetListeningForNewDevice(true);
        LobbyData!.UnsetAllTeams();
        LobbyData.LocalDespawnAll();
        LobbyData.InitializeData();
        Transitions.SetLoading();
        Transitions.ToVersus(() =>
        {
            Transitions.ToGameplay(() =>
            {
                callback?.Invoke();
                ReloadedClientData.LocalClient?.Ready.Value = true;
            });
        });
    }

    /// <summary>
    /// Creates a new lobby with a maximum of 2 players (Versus mode).
    /// </summary>
    internal static void CreateLobby()
    {
        NetworkTransport!.CreateLobby(MAX_LOBBY_SIZE);
        Transitions.SetLoading();
    }

    /// <summary>
    /// Joins an existing lobby.
    /// </summary>
    internal static void JoinLobby(ID lobbyId)
    {
        NetworkTransport!.JoinLobby(lobbyId);
        Transitions.SetLoading();
        ReplantedOnlineMod.Logger.Msg(typeof(ReloadedLobby), $"Joining lobby: {lobbyId}");
    }

    /// <summary>
    /// Leaves the current lobby and cleans up network connections.
    /// </summary>
    internal static void LeaveLobby(Action? callback = null)
    {
        if (LobbyData == null)
        {
            ReplantedOnlineMod.Logger.Warning(typeof(ReloadedLobby), "Cannot leave - not in a lobby");
            return;
        }

        ReplantedOnlineMod.Logger.Msg(typeof(ReloadedLobby), $"Leaving lobby {LobbyData.LobbyId}");
        var lobbyId = LobbyData.LobbyId;
        LobbyData.Dispose();
        Transitions.SetLoading();
        Transitions.ToMainMenu(() =>
        {
            SteamClientPatch.TryClearTempApp();
            callback?.Invoke();
        });
        LobbyData = null;
        NetworkTransport!.LeaveLobby(lobbyId);
        DiscordManager.OnLeftLobby();
        ReplantedOnlineMod.Logger.Msg(typeof(ReloadedLobby), "Successfully left lobby");
    }

    internal static void OnLobbyCreatedCompleted(Result result, ServerLobby lobby)
    {
        if (result == Result.OK)
        {
            LobbyData?.Dispose();
            LobbyData = new(lobby, lobby.Id, lobby.OwnerId);
            LobbyData.InitializeData();
            ReplantedOnlineMod.Logger.Msg(typeof(ReloadedLobby), $"Lobby created successfully: {LobbyData.LobbyId}");
            ReloadedMatchmaking.SetLobbyData(LobbyData);
        }
        else
        {
            Transitions.ToMainMenu();
            ReplantedOnlineMod.Logger.Error(typeof(ReloadedLobby), $"Lobby creation failed with result: {result}");
        }
    }

    internal static void OnLobbyEnteredCompleted(ServerLobby lobby)
    {
        LobbyData?.Dispose();
        LobbyData = new(lobby, lobby.Id, lobby.OwnerId);
        LobbyData.LobbyCode = NetworkTransport!.GetLobbyData(LobbyData.LobbyId, ReplantedOnlineMod.Constants.Network.GAME_CODE_KEY);

        int memberCount = GetLobbyMemberCount();
        for (int i = 0; i < memberCount; i++)
        {
            var member = GetLobbyMemberByIndex(i);
            LobbyData.OnClientJoined(member);

        }

        Transitions.ToVersus(() =>
        {
            NetworkManager.StartListening();
            LobbyData.UpdateLobbyStates();
            ReloadedClientData.LocalClient?.Ready.Value = true;
        });
        DiscordManager.OnJoinLobby();

        if (memberCount > 1)
        {
            ReplantedOnlineMod.Logger.Msg(typeof(ReloadedLobby), $"Joined lobby {LobbyData.LobbyId} with {memberCount} players");
        }
        else
        {
            ReplantedOnlineMod.Logger.Msg(typeof(ReloadedLobby), $"Joined lobby {LobbyData.LobbyId} with {memberCount} player");
        }
    }

    internal static void OnLobbyDataChanged(ServerLobby lobby)
    {
        if (!AmInLobby()) return;

        LobbyData!.UpdateLobbyStates();
    }

    internal static void OnLobbyMemberJoined(ServerLobby lobby, ID clientId)
    {
        if (lobby.Id != LobbyData!.LobbyId)
        {
            ReplantedOnlineMod.Logger.Warning(typeof(ReloadedLobby), $"Member joined different lobby (ours: {LobbyData.LobbyId}, theirs: {lobby.Id})");
            return;
        }

        if (clientId.IsBanned())
        {
            return;
        }

        ReplantedOnlineMod.Logger.Msg(typeof(ReloadedLobby), $"Player {clientId} ({NetworkTransport!.GetMemberName(clientId)}) joined the lobby");
        LobbyData?.OnClientJoined(clientId);

        // If we're the host, request P2P session with the new player
        if (AmLobbyHost())
        {
            ReplantedOnlineMod.Logger.Msg(typeof(ReloadedLobby), $"Host initiating P2P connection with new player {clientId}");
            NetworkManager.Packet<NetworkObjectSpawnPacket>.Singleton.SendNetworkObjectsTo(clientId);
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
        else
        {
            if (lobby.OwnerId == clientId)
            {
                LeaveLobby(() =>
                {
                    CustomPopupPanel.Show("Disconnected", "Host has left the game!");
                });
            }
        }

        LobbyData?.OnClientLeft(clientId);
    }

    internal static void OnP2PSessionRequest(ID clientId)
    {
        if (clientId.IsBanned()) return;

        if (IsPlayerInOurLobby(clientId))
        {
            NetworkTransport!.AcceptP2PSessionWithUser(clientId);
        }
    }

    /// <summary>
    /// Try to remove player from the lobby, if not the P2P will terminate ether way
    /// </summary>
    /// <param name="clientId">The CLient ID of the player to remove.</param>
    /// <param name="reason">The reason for the bam.</param>
    internal static void BanFromLobby(ID clientId, BanReason reason)
    {
        if (!AmInLobby())
        {
            ReplantedOnlineMod.Logger.Warning(typeof(ReloadedLobby), "Cannot kick player - not in a lobby");
            return;
        }

        if (!AmLobbyHost())
        {
            ReplantedOnlineMod.Logger.Warning(typeof(ReloadedLobby), "Only the lobby host can kick players");
            return;
        }

        if (clientId == NetworkTransport!.LocalClientId)
        {
            ReplantedOnlineMod.Logger.Warning(typeof(ReloadedLobby), "Cannot kick yourself");
            return;
        }

        if (!IsPlayerInOurLobby(clientId))
        {
            ReplantedOnlineMod.Logger.Warning(typeof(ReloadedLobby), $"Player {clientId} is not in the lobby");
            return;
        }

        NetworkManager.Packet<RemoveClientPacket>.Singleton.Send(clientId, reason);

        clientId.Ban();
    }

    /// <summary>
    /// Gets the number of clients currently in the lobby.
    /// </summary>
    /// <returns>The number of lobby clients.</returns>
    internal static int GetLobbyClientCount()
    {
        return LobbyData!.AllClients.Count;
    }

    /// <summary>
    /// Gets the number of members currently in the lobby.
    /// </summary>
    /// <returns>The number of lobby members.</returns>
    internal static int GetLobbyMemberCount()
    {
        return NetworkTransport!.GetNumLobbyMembers(LobbyData!.LobbyId);
    }

    /// <summary>
    /// Gets the Client ID of a lobby member by their index.
    /// </summary>
    /// <param name="index">The zero-based index of the lobby member.</param>
    /// <returns>The Client ID of the lobby member at the specified index.</returns>
    internal static ID GetLobbyMemberByIndex(int index)
    {
        return NetworkTransport!.GetLobbyMemberByIndex(LobbyData!.LobbyId, index);
    }

    /// <summary>
    /// Gets the Client ID of the lobby owner.
    /// </summary>
    /// <returns>The CLient ID of the lobby owner.</returns>
    internal static ID GetLobbyOwner()
    {
        return NetworkTransport!.GetLobbyOwner(LobbyData!.LobbyId);
    }

    /// <summary>
    /// Checks if a player is currently in our lobby.
    /// </summary>
    /// <param name="clientId">The Client ID of the player to check.</param>
    /// <returns>True if the player is in our lobby, false otherwise.</returns>
    internal static bool IsPlayerInOurLobby(ID clientId)
    {
        foreach (var client in LobbyData!.AllClients.Values)
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
        return GetLobbyOwner() == NetworkTransport!.LocalClientId;
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
        return NetworkTransport!.GetLobbyData(LobbyData!.LobbyId, ReplantedOnlineMod.Constants.Network.GAME_CODE_KEY);
    }
}
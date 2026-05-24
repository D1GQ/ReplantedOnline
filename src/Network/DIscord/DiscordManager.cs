using DiscordRPC;
using DiscordRPC.Message;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Managers.Network;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded.Panel;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.MonoScripts.Unity;
using ReplantedOnline.Network.Client;
using UnityEngine.SceneManagement;

namespace ReplantedOnline.Network.Discord;

/// <summary>
/// Manages Discord Rich Presence integration for the game, handling lobby status,
/// gameplay state, and join requests.
/// </summary>
internal static class DiscordManager
{
    private const string DISCORD_APPID = "1505741829071573094";

    private static DiscordRpcClient _client;
    private static RichPresence _presence;
    private static DiscordLobbySecret _discordLobbySecret;

    private static bool _init;
    private static readonly DateTime _start = DateTime.UtcNow;

    private static string _details = "";
    private static string _state = "";

    private static DateTime? _time;
    private static int _timeState = -1;

    private static bool _dirty;

    private static int _lastPartySize = -1;

    /// <summary>
    /// Initializes the Discord RPC client and sets up the initial presence.
    /// </summary>
    internal static void Initialize()
    {
        _client = new DiscordRpcClient(DISCORD_APPID);

        _client.RegisterUriScheme();

        _client.OnJoin += OnJoin;
        _client.OnJoinRequested += OnJoinRequested;

        _client.Initialize();

        _presence = new RichPresence
        {
            Assets = new Assets(),
            Timestamps = new Timestamps(),
            Type = ActivityType.Playing,
            Details = "Loading Screen",
            State = "",
            Secrets = new(),
            Party = new()
            {
                ID = string.Empty,
                Size = 0,
                Max = ReloadedLobby.MAX_LOBBY_SIZE,
                Privacy = Party.PrivacySetting.Public
            }
        };

        _presence.Timestamps.Start = _start;

        _client.SetPresence(_presence);

        _init = true;
    }

    /// <summary>
    /// Subscribes to join and join request events when the client is ready to receive them.
    /// </summary>
    internal static void ReadyToJoin()
    {
        _client.Subscribe(EventType.Join | EventType.JoinRequest);
    }

    /// <summary>
    /// Handles join events by searching for a Steam lobby with the provided game code.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="args">The join message containing the secret/game code.</param>
    private static void OnJoin(object sender, JoinMessage args)
    {
        MainThreadDispatcher.Execute(() =>
        {
            _discordLobbySecret = DiscordLobbySecret.Deserialize(args.Secret);
            if (_discordLobbySecret == null)
            {
                CustomPopupPanel.Show("Disconnected", $"Failed to read secret!");
                return;
            }

            if (_discordLobbySecret.VersionFormatted != ModInfo.MOD_VERSION_FORMATTED)
            {
                CustomPopupPanel.Show("Disconnected", $"Unable to join due to mod version mismatch\nv{_discordLobbySecret.VersionFormatted}");
                return;
            }

            MatchmakingManager.SearchSteamLobbyByGameCode(_discordLobbySecret.GameCode);
        });
    }

    /// <summary>
    /// Handles join request events by accepting or rejecting based on lobby joinability.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="args">The join request message containing user information.</param>
    private static void OnJoinRequested(object sender, JoinRequestMessage args)
    {
        bool accept = ReloadedLobby.LobbyData.LobbyJoinable.Value;
        _client.Respond(args, accept);
    }

    /// <summary>
    /// Updates the Discord presence state and party size. Should be called regularly.
    /// </summary>
    internal static void Update()
    {
        if (ReloadedLobby.AmInLobby())
        {
            int size = ReloadedLobby.GetLobbyMemberCount();
            if (size != _lastPartySize)
            {
                _lastPartySize = size;
                _client.UpdatePartySize(size);
                _dirty = true;
            }
        }

        UpdateState();

        if (_dirty)
        {
            _dirty = false;
            _client.SetPresence(_presence);
            _client.Invoke();
        }
    }

    /// <summary>
    /// Updates the presence details and state based on current game and lobby status.
    /// </summary>
    private static void UpdateState()
    {
        if (!_init) return;

        string newDetails = "";
        string newState = "";
        ActivityType type = ActivityType.Playing;

        bool inLobby = ReloadedLobby.AmInLobby();

        if (!inLobby)
        {
            string scene = SceneManager.GetActiveScene().name;

            if (scene == "Frontend")
            {
                newDetails = "Main Menu";
            }
            else if (scene == "Gameplay")
            {
                var entry = Instances.GameplayActivity?.Board?.mLevelEntryData;
                if (entry != null)
                {
                    newDetails = "Gameplay";
                    newState = entry.FullLevelName;
                }
            }
        }
        else
        {
            newState = "";

            if (ReloadedLobby.lastTransportMode == 1)
                newDetails += "LAN: ";

            switch (VersusState.VersusPhase)
            {
                case VersusPhase.PickSides:
                    newDetails += "Versus Mode";
                    newState = "In Lobby";
                    break;

                case VersusPhase.ChoosePlantPacket:
                case VersusPhase.ChooseZombiePacket:
                    newDetails += "Versus Mode";
                    newState = "Choosing Seeds";
                    break;

                case VersusPhase.Gameplay:
                    newDetails += "Versus Mode";
                    type = ActivityType.Competing;
                    break;

                case VersusPhase.SuddenDeath:
                    newDetails += "Sudden Death!";
                    type = ActivityType.Competing;
                    break;

                case VersusPhase.PlantsWin:
                case VersusPhase.ZombiesWin:
                    newDetails += "Versus Mode";
                    newState = "Results";
                    break;
            }
        }

        bool changed = newDetails != _details || newState != _state;

        if (!changed) return;

        _details = newDetails;
        _state = newState;

        SetTimestamp(newDetails);

        _presence.Type = type;
        _presence.Details = _details;
        _presence.State = _state;
        _presence.Timestamps.Start = _time;

        _dirty = true;
    }

    /// <summary>
    /// Sets the timestamp for the Discord presence based on the current screen.
    /// </summary>
    /// <param name="details">The current presence details string.</param>
    private static void SetTimestamp(string details)
    {
        if (details is "Loading Screen" or "Main Menu")
        {
            if (_timeState != 0)
            {
                _timeState = 0;
                _time = _start;
            }
        }
        else
        {
            if (_timeState != 1)
            {
                _timeState = 1;
                _time = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Called when the lobby is restarted to update the party ID.
    /// </summary>
    internal static void OnLobbyRestart()
    {
        _presence.Party.ID = Secrets.CreateFriendlySecret(new());
        _client.SetPresence(_presence);
        _client.Invoke();
    }

    /// <summary>
    /// Called when joining a lobby to set the party ID and join secret.
    /// </summary>
    internal static void OnJoinLobby()
    {
        _presence.Party.ID = Secrets.CreateFriendlySecret(new());
        _discordLobbySecret = new DiscordLobbySecret()
        {
            VersionFormatted = ModInfo.MOD_VERSION_FORMATTED,
            GameCode = ReloadedLobby.GetCurrentLobbyGameCode()
        };
        _presence.Secrets.Join = _discordLobbySecret.Serialize();
        _client.SetPresence(_presence);
        _client.Invoke();
        _lastPartySize = -1;
    }

    /// <summary>
    /// Called when leaving a lobby to clear the party ID and join secret.
    /// </summary>
    internal static void OnLeftLobby()
    {
        _presence.Party.ID = string.Empty;
        _presence.Secrets.Join = string.Empty;
        _client.SetPresence(_presence);
        _client.Invoke();
    }

    /// <summary>
    /// Disposes the Discord RPC client and clears the presence.
    /// </summary>
    internal static void Dispose()
    {
        if (!_init) return;

        _client.ClearPresence();
        _client.Dispose();
        _init = false;
    }
}
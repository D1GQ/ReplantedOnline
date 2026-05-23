using DiscordRPC;
using DiscordRPC.Message;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Client;
using UnityEngine.SceneManagement;

namespace ReplantedOnline.Managers.Network;

internal static class DiscordManager
{
    private const string DISCORD_APPID = "1505741829071573094";

    private static DiscordRpcClient _client;
    private static RichPresence _presence;

    private static bool _init;
    private static readonly DateTime _start = DateTime.UtcNow;

    private static string _details = "";
    private static string _state = "";

    private static DateTime? _time;
    private static int _timeState = -1;

    private static bool _dirty;

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

    internal static void ReadyToJoin()
    {
        _client.Subscribe(EventType.Join | EventType.JoinRequest);
    }

    private static void OnJoin(object sender, JoinMessage args)
    {
        MatchmakingManager.SearchSteamLobbyByGameCode(args.Secret);
    }

    private static void OnJoinRequested(object sender, JoinRequestMessage args)
    {
        bool accept = ReloadedLobby.LobbyData.LobbyJoinable.Value;
        _client.Respond(args, accept);
    }

    private static int _lastPartySize = -1;
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

    internal static void OnLobbyRestart()
    {
        _presence.Party.ID = Secrets.CreateFriendlySecret(new());
        _client.SetPresence(_presence);
        _client.Invoke();
    }

    internal static void OnJoinLobby()
    {
        _presence.Party.ID = Secrets.CreateFriendlySecret(new());
        _presence.Secrets.Join = ReloadedLobby.GetCurrentLobbyGameCode();
        _client.SetPresence(_presence);
        _client.Invoke();
        _lastPartySize = -1;
    }

    internal static void OnLeftLobby()
    {
        _presence.Party.ID = string.Empty;
        _presence.Secrets.Join = string.Empty;
        _client.SetPresence(_presence);
        _client.Invoke();
    }

    internal static void Dispose()
    {
        if (!_init) return;

        _client.ClearPresence();
        _client.Dispose();
        _init = false;
    }
}
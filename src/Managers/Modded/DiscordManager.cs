using DiscordRPC;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Client;
using UnityEngine.SceneManagement;

namespace ReplantedOnline.Managers.Modded;

/// <summary>
/// Manages Discord Rich Presence integration for Replanted Online.
/// </summary>
internal static class DiscordManager
{
    private const string DISCORD_APPID = "1505653063623512114";
    private static DiscordRpcClient _client;
    private static RichPresence _richPresence;
    private static Assets _assets;
    private static bool _init;
    private static readonly DateTime _start = DateTime.UtcNow;

    /// <summary>
    /// Initializes the Discord Rich Presence client and sets up the initial presence.
    /// </summary>
    internal static void Initialize()
    {
        _client = new(DISCORD_APPID);
        _richPresence = new();
        _assets = new();
        _richPresence.Assets = _assets;
        _richPresence.Details = "Loading Screen";
        _richPresence.Timestamps = new Timestamps();

        _client.Initialize();
        _client.SetPresence(_richPresence);

        _init = true;
    }

    private static string _details = string.Empty;
    private static DateTime? _time;

    /// <summary>
    /// Updates the Discord presence based on current game state.
    /// </summary>
    internal static void Update()
    {
        if (!_init) return;

        if (!ReloadedLobby.AmInLobby())
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "Frontend")
            {
                _details = "Main Menu";
            }
            else if (sceneName == "Gameplay")
            {
                _details = "Gameplay";
            }
        }
        else
        {
            _details = string.Empty;
            if (ReloadedLobby.lastTransportMode == 1)
            {
                _details += "LAN: ";
            }

            switch (VersusState.VersusPhase)
            {
                case VersusPhase.PickSides:
                    _details += "Versus In Lobby";
                    break;
                case VersusPhase.ChoosePlantPacket:
                case VersusPhase.ChooseZombiePacket:
                    _details += "Versus Choosing Seeds";
                    break;
                case VersusPhase.Gameplay:
                    _details += "Versus Gameplay";
                    break;
                case VersusPhase.SuddenDeath:
                    _details += "Versus Sudden Death";
                    break;
                case VersusPhase.PlantsWin:
                case VersusPhase.ZombiesWin:
                    _details += "Versus Results";
                    break;
            }
        }

        if (_details != _richPresence.Details)
        {
            SetTime();
            _richPresence.Details = _details;
            _richPresence.Timestamps.Start = _time;
            _client.SetPresence(_richPresence);
        }
    }

    private static int _timeState = -1;

    /// <summary>
    /// Sets the timestamp for the current presence state.
    /// </summary>
    private static void SetTime()
    {
        switch (_details)
        {
            case "Loading Screen":
            case "Main Menu":
                if (_timeState != 0)
                {
                    _timeState = 0;
                    _time = _start;
                }
                break;
            default:
                if (_timeState != 1)
                {
                    _timeState = 1;
                    _time = DateTime.UtcNow;
                }
                break;
        }
    }

    /// <summary>
    /// Disposes of the Discord RPC client and clears the presence.
    /// </summary>
    internal static void Dispose()
    {
        if (!_init) return;

        _client.ClearPresence();
        _client.Dispose();
    }
}
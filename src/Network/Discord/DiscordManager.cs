using DiscordRPC;
using DiscordRPC.Message;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded.Panel;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.MonoScripts.Unity;
using ReplantedOnline.Network.Reloaded.Client;
using UnityEngine.SceneManagement;

namespace ReplantedOnline.Network.Discord;

/// <summary>
/// Manages Discord Rich Presence integration for the game.
/// </summary>
internal static class DiscordManager
{
    private const string DiscordAppId = "1505741829071573094";

    private const string LoadingScreenDetails = "Loading Screen";
    private const string MainMenuDetails = "Main Menu";

    private static readonly DateTime StartTime = DateTime.UtcNow;

    private static readonly (string Details, string State, ActivityType Type) NoActivity =
        (string.Empty, string.Empty, ActivityType.Playing);

    private static DiscordRpcClient Client = default!;
    private static RichPresence Presence = default!;

    // Last values applied to the presence, used to detect changes.
    private static string Details = string.Empty;
    private static string State = string.Empty;
    private static DateTime? ActivityStartTime;

    private static bool MenuTimestamp = true;
    private static int LastPartySize = -1;
    private static bool Initialized;
    private static bool Dirty;

    /// <summary>
    /// Initializes the Discord RPC client and sets up the initial presence.
    /// </summary>
    internal static void Initialize()
    {
        if (Initialized)
            return;

        Client = new DiscordRpcClient(DiscordAppId);
        Client.RegisterUriScheme();

        Client.OnJoin += OnJoin;
        Client.OnJoinRequested += OnJoinRequested;

        Client.Initialize();

        Presence = new RichPresence
        {
            Type = ActivityType.Playing,
            Details = LoadingScreenDetails,
            State = string.Empty,
            Assets = new Assets(),
            Timestamps = new Timestamps { Start = StartTime },
            Secrets = new Secrets(),
            Party = new Party
            {
                ID = string.Empty,
                Size = 0,
                Max = ReloadedLobby.MAX_LOBBY_SIZE,
                Privacy = Party.PrivacySetting.Public
            }
        };

        PushPresence();

        Initialized = true;
    }

    /// <summary>
    /// Subscribes to Discord join and join-request events.
    /// </summary>
    internal static void ReadyToJoin()
    {
        Client.Subscribe(EventType.Join | EventType.JoinRequest);
    }

    /// <summary>
    /// Refreshes the presence state and pushes it to Discord if anything changed.
    /// </summary>
    internal static void Update()
    {
        if (!Initialized)
            return;

        UpdatePartySize();
        UpdateActivity();

        if (!Dirty)
            return;

        PushPresence();
        Dirty = false;
    }

    private static void UpdatePartySize()
    {
        if (!ReloadedLobby.AmInLobby() || !Presence.HasParty())
            return;

        int size = ReloadedLobby.GetLobbyClientCount();
        if (size == LastPartySize)
            return;

        LastPartySize = size;
        Presence.Party.Size = size;
        Dirty = true;
    }

    private static void UpdateActivity()
    {
        var (details, state, type) = GetActivity();

        if (details == Details && state == State && type == Presence.Type)
            return;

        Details = details;
        State = state;

        UpdateTimestamp(details);

        Presence.Details = details;
        Presence.State = state;
        Presence.Type = type;
        Presence.Timestamps.Start = ActivityStartTime;

        Dirty = true;
    }

    private static (string Details, string State, ActivityType Type) GetActivity()
    {
        return ReloadedLobby.AmInLobby() ? GetLobbyActivity() : GetOfflineActivity();
    }

    private static (string Details, string State, ActivityType Type) GetOfflineActivity()
    {
        switch (SceneManager.GetActiveScene().name)
        {
            case "Frontend":
                return (MainMenuDetails, string.Empty, ActivityType.Playing);

            case "Gameplay":
                var entry = Instances.GameplayActivity?.Board?.mLevelEntryData;
                return entry != null
                    ? ("Gameplay", entry.FullLevelName, ActivityType.Playing)
                    : NoActivity;

            default:
                return NoActivity;
        }
    }

    private static (string Details, string State, ActivityType Type) GetLobbyActivity()
    {
        string prefix = ReloadedLobby.TransportMode == TransportMode.Lan ? "LAN: " : string.Empty;
        string versus = $"{prefix}Versus Mode";

        return VersusState.VersusPhase switch
        {
            VersusPhase.PickSides => (versus, "In Lobby", ActivityType.Playing),

            VersusPhase.ChoosePlantPacket or
            VersusPhase.ChooseZombiePacket => (versus, "Choosing Seeds", ActivityType.Playing),

            VersusPhase.Gameplay => (versus, string.Empty, ActivityType.Competing),

            VersusPhase.SuddenDeath => ($"{prefix}Sudden Death!", string.Empty, ActivityType.Competing),

            VersusPhase.PlantsWin or
            VersusPhase.ZombiesWin => (versus, "Results", ActivityType.Playing),

            _ => NoActivity
        };
    }

    /// <summary>
    /// Switches the timer between "since launch" (menus) and "since now" (activities).
    /// </summary>
    private static void UpdateTimestamp(string details)
    {
        bool isMenu = details is LoadingScreenDetails or MainMenuDetails;

        if (MenuTimestamp == isMenu)
            return;

        MenuTimestamp = isMenu;
        ActivityStartTime = isMenu ? StartTime : DateTime.UtcNow;
    }

    private static void PushPresence()
    {
        Client.SetPresence(Presence);
        Client.Invoke();
    }

    private static void OnJoin(object sender, JoinMessage args)
    {
        MainThreadDispatcher.Execute(() =>
        {
            if (ReloadedLobby.AmInLobby())
                return;

            if (!DiscordLobbySecret.TryDeserialize(args.Secret, out var secret))
            {
                CustomPopupPanel.ShowLocalized("$POPUP_HEADER_DISCONNECTED", "$POPUP_FAILED_TO_READ_SECRET");
                return;
            }

            if (secret.ModSignatureHash != ReplantedOnlineMod.ModInfo.ModSignature.SignatureHash)
            {
                CustomPopupPanel.ShowLocalized("$POPUP_HEADER_DISCONNECTED", "$POPUP_SECRET_HASH_MISMATCH");
                return;
            }

            if (secret.VersionFormatted != ReplantedOnlineMod.ModInfo.MOD_VERSION_FORMATTED)
            {
                CustomPopupPanel.ShowLocalized("$POPUP_HEADER_DISCONNECTED", "$POPUP_VERSION_MISMATCH", secret.VersionFormatted);
                return;
            }

            ReloadedMatchmaking.JoinSteamLobbyByGameCode(secret.GameCode);
        });
    }

    private static void OnJoinRequested(object sender, JoinRequestMessage args)
    {
        if (ReloadedLobby.LobbyData == null)
            return;

        bool accept = ReloadedLobby.LobbyData.LobbyJoinable.Value;
        Client.Respond(args, accept);

        if (accept)
            Dirty = true;
    }

    /// <summary>
    /// Called when the lobby is restarted to refresh the party ID.
    /// </summary>
    internal static void OnLobbyRestart()
    {
        if (ReloadedLobby.TransportMode != TransportMode.Steam)
            return;

        RefreshPartyId();
        Dirty = true;
    }

    /// <summary>
    /// Called when joining a lobby to set up the party ID and join secret.
    /// </summary>
    internal static void OnJoinLobby()
    {
        if (ReloadedLobby.TransportMode != TransportMode.Steam)
            return;

        RefreshPartyId();

        Presence.Secrets.Join = new DiscordLobbySecret
        {
            ModSignatureHash = ReplantedOnlineMod.ModInfo.ModSignature.SignatureHash,
            VersionFormatted = ReplantedOnlineMod.ModInfo.MOD_VERSION_FORMATTED,
            GameCode = ReloadedLobby.GetCurrentLobbyGameCode()
        }.Serialize();

        LastPartySize = -1;
        Dirty = true;
    }

    /// <summary>
    /// Called when leaving a lobby to clear party information.
    /// </summary>
    internal static void OnLeftLobby()
    {
        Presence.Party.ID = string.Empty;
        Presence.Secrets.Join = string.Empty;
        Dirty = true;
    }

    /// <summary>
    /// Sets whether the current lobby is joinable by other players.
    /// </summary>
    internal static void SetJoinable(bool canJoin)
    {
        Presence.Party.Privacy = canJoin ? Party.PrivacySetting.Public : Party.PrivacySetting.Private;
        Dirty = true;
    }

    /// <summary>
    /// Disposes of the Discord RPC client and cleans up resources.
    /// </summary>
    internal static void Dispose()
    {
        if (!Initialized)
            return;

        Client.OnJoin -= OnJoin;
        Client.OnJoinRequested -= OnJoinRequested;
        Client.Unsubscribe(EventType.Join | EventType.JoinRequest);
        Client.ClearPresence();
        Client.Dispose();

        Initialized = false;
    }

    private static void RefreshPartyId()
    {
        Presence.Party.ID = Secrets.CreateFriendlySecret(new());
    }
}
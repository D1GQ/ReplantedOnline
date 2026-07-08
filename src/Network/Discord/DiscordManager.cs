using DiscordRPC;
using DiscordRPC.Message;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded.Panel;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.MonoScripts.Unity;
using ReplantedOnline.Network.Client;
using UnityEngine.SceneManagement;

namespace ReplantedOnline.Network.Discord;

/// <summary>
/// Manages Discord Rich Presence integration for the game.
/// </summary>
internal static class DiscordManager
{
    /// <summary>
    /// The Discord application ID used for Rich Presence.
    /// </summary>
    private const string DiscordAppId = "1505741829071573094";

    /// <summary>
    /// The UTC time when the Discord manager was initialized.
    /// </summary>
    private static readonly DateTime StartTime = DateTime.UtcNow;

    /// <summary>
    /// The Discord RPC client instance.
    /// </summary>
    private static DiscordRpcClient Client = default!;

    /// <summary>
    /// The current Rich Presence data.
    /// </summary>
    private static RichPresence Presence = default!;

    /// <summary>
    /// The current presence details text.
    /// </summary>
    private static string Details = string.Empty;

    /// <summary>
    /// The current presence state text.
    /// </summary>
    private static string State = string.Empty;

    /// <summary>
    /// The start time for the current activity.
    /// </summary>
    private static DateTime? ActivityStartTime;

    /// <summary>
    /// Indicates whether the menu timestamp should be used.
    /// </summary>
    private static bool MenuTimestamp = true;

    /// <summary>
    /// The last recorded party size to avoid unnecessary updates.
    /// </summary>
    private static int LastPartySize = -1;

    /// <summary>
    /// Indicates whether the Discord client has been initialized.
    /// </summary>
    private static bool Initialized;

    /// <summary>
    /// Indicates whether the presence data has changed and needs to be pushed.
    /// </summary>
    private static bool Dirty;

    /// <summary>
    /// Initializes the Discord RPC client and sets up the initial presence.
    /// </summary>
    internal static void Initialize()
    {
        if (Initialized) return;

        Client = new DiscordRpcClient(DiscordAppId);

        Client.RegisterUriScheme();

        Client.OnJoin += OnJoin;
        Client.OnJoinRequested += OnJoinRequested;

        Client.Initialize();

        Presence = new RichPresence
        {
            Type = ActivityType.Playing,
            Details = "Loading Screen",
            State = string.Empty,

            Assets = new Assets(),

            Timestamps = new Timestamps
            {
                Start = StartTime
            },

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
    /// Subscribes to Discord events for join requests and join functionality.
    /// </summary>
    internal static void ReadyToJoin()
    {
        Client.Subscribe(EventType.Join | EventType.JoinRequest);
    }

    /// <summary>
    /// Updates the Discord presence state and pushes changes if necessary.
    /// </summary>
    internal static void Update()
    {
        if (!Initialized) return;

        UpdatePartySize();
        UpdateActivity();

        if (Dirty)
        {
            PushPresence();
            Dirty = false;
        }
    }

    /// <summary>
    /// Updates the party size in the Rich Presence if it has changed.
    /// </summary>
    private static void UpdatePartySize()
    {
        if (!ReloadedLobby.AmInLobby()) return;
        if (!Presence.HasParty()) return;

        int size = ReloadedLobby.GetLobbyMemberCount();
        if (size == LastPartySize) return;

        LastPartySize = size;
        Presence.Party.Size = size;
        Dirty = true;
    }

    /// <summary>
    /// Updates the activity details, state, and type based on current game state.
    /// </summary>
    private static void UpdateActivity()
    {
        GetActivity(out string details, out string state, out ActivityType type);

        if (Details == details && State == state && Presence.Type == type)
        {
            return;
        }

        Details = details;
        State = state;

        UpdateTimestamp(details);

        Presence.Details = details;
        Presence.State = state;
        Presence.Type = type;
        Presence.Timestamps.Start = ActivityStartTime;

        Dirty = true;
    }

    /// <summary>
    /// Determines the current activity details, state, and type based on game state.
    /// </summary>
    /// <param name="details">The output details text for the presence.</param>
    /// <param name="state">The output state text for the presence.</param>
    /// <param name="type">The output activity type for the presence.</param>
    private static void GetActivity(out string details, out string state, out ActivityType type)
    {
        details = string.Empty;
        state = string.Empty;
        type = ActivityType.Playing;

        if (!ReloadedLobby.AmInLobby())
        {
            switch (SceneManager.GetActiveScene().name)
            {
                case "Frontend":
                    details = "Main Menu"; return;

                case "Gameplay":
                    {
                        var entry = Instances.GameplayActivity?.Board?.mLevelEntryData;

                        if (entry != null)
                        {
                            details = "Gameplay";
                            state = entry.FullLevelName;
                        }

                        return;
                    }
            }

            return;
        }

        bool lan = ReloadedLobby.TransportMode == TransportMode.Lan;

        string prefix = lan ? "LAN: " : string.Empty;

        switch (VersusState.VersusPhase)
        {
            case VersusPhase.PickSides:
                details = $"{prefix}Versus Mode";
                state = "In Lobby";
                break;

            case VersusPhase.ChoosePlantPacket:
            case VersusPhase.ChooseZombiePacket:
                details = $"{prefix}Versus Mode";
                state = "Choosing Seeds";
                break;

            case VersusPhase.Gameplay:
                details = $"{prefix}Versus Mode";
                type = ActivityType.Competing;
                break;

            case VersusPhase.SuddenDeath:
                details = $"{prefix}Sudden Death!";
                type = ActivityType.Competing;
                break;

            case VersusPhase.PlantsWin:
            case VersusPhase.ZombiesWin:
                details = $"{prefix}Versus Mode";
                state = "Results";
                break;
        }
    }

    /// <summary>
    /// Updates the activity timestamp based on whether the user is in a menu or gameplay.
    /// </summary>
    /// <param name="details">The current activity details used to determine menu state.</param>
    private static void UpdateTimestamp(string details)
    {
        bool menuState = details is "Loading Screen" or "Main Menu";

        if (MenuTimestamp == menuState) return;

        MenuTimestamp = menuState;

        ActivityStartTime = menuState ? StartTime : DateTime.UtcNow;
    }

    /// <summary>
    /// Pushes the current Rich Presence data to Discord.
    /// </summary>
    private static void PushPresence()
    {
        Client.SetPresence(Presence);
        Client.Invoke();
    }

    /// <summary>
    /// Handles a Discord join event, attempting to join a lobby using the provided secret.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="args">The join message containing the lobby secret.</param>
    private static void OnJoin(object sender, JoinMessage args)
    {
        MainThreadDispatcher.Execute(() =>
        {
            DiscordLobbySecret secret = DiscordLobbySecret.Deserialize(args.Secret);

            if (secret.FormatError)
            {
                CustomPopupPanel.Show("Disconnected", "Failed to read secret!");
                return;
            }

            if (secret.ModSignatureHash != ModInfo.ModSignature.SignatureHash)
            {
                CustomPopupPanel.Show("Disconnected", "Failed to read secret due to hash!");
                return;
            }

            if (secret.VersionFormatted != ModInfo.MOD_VERSION_FORMATTED)
            {
                CustomPopupPanel.Show("Disconnected", $"Unable to join due to mod version mismatch\nv{secret.VersionFormatted}");

                return;
            }



            ReloadedMatchmaking.SearchSteamLobbyByGameCode(secret.GameCode);
        });
    }

    /// <summary>
    /// Handles a Discord join request event, accepting or rejecting based on lobby joinability.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="args">The join request message containing user information.</param>
    private static void OnJoinRequested(object sender, JoinRequestMessage args)
    {
        if (ReloadedLobby.LobbyData == null) return;
        bool accept = ReloadedLobby.LobbyData.LobbyJoinable.Value;
        Client.Respond(args, accept);
        if (accept)
        {
            Dirty = true;
        }
    }

    /// <summary>
    /// Called when the lobby is restarted to refresh the party ID.
    /// </summary>
    internal static void OnLobbyRestart()
    {
        if (ReloadedLobby.TransportMode == TransportMode.Lan) return;

        Presence.Party.ID = Secrets.CreateFriendlySecret(new());
        Dirty = true;
    }

    /// <summary>
    /// Called when joining a lobby to set up the party ID and join secret.
    /// </summary>
    internal static void OnJoinLobby()
    {
        if (ReloadedLobby.TransportMode == TransportMode.Lan) return;

        Presence.Party.ID = Secrets.CreateFriendlySecret(new());

        DiscordLobbySecret secret = new()
        {
            ModSignatureHash = ModInfo.ModSignature.SignatureHash,
            VersionFormatted = ModInfo.MOD_VERSION_FORMATTED,
            GameCode = ReloadedLobby.GetCurrentLobbyGameCode()
        };

        Presence.Secrets.Join = secret.Serialize();

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
    /// <param name="canJoin">True if the lobby should be joinable, false otherwise.</param>
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
        if (!Initialized) return;

        Client.OnJoin -= OnJoin;
        Client.OnJoinRequested -= OnJoinRequested;
        Client.Unsubscribe(EventType.Join | EventType.JoinRequest);
        Client.ClearPresence();
        Client.Dispose();

        Initialized = false;
    }
}
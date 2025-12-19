using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSteamworks;
using Il2CppSteamworks.Data;
using MelonLoader;
using ReplantedOnline.Enums;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using System.Text;

namespace ReplantedOnline.Managers;

/// <summary>
/// Manages Steam matchmaking functionality for finding and joining multiplayer lobbies in ReplantedOnline.
/// Handles lobby searching by game codes and generates consistent lobby identifiers.
/// </summary>
internal static class MatchmakingManager
{
    /// <summary>
    /// Gets the full version string combining the release type and version number.
    /// </summary>
    internal static string Version_Check => $"{ModInfo.MOD_RELEASE} v{ModInfo.MOD_VERSION}";

    /// <summary>
    /// Character set used for generating game codes. Excludes confusing characters like O/0 and I/1.
    /// </summary>
    internal static readonly char[] CODE_CHARS = "ABCDEFHIJKLMNPQRSTUVWXYZ".ToCharArray();

    /// <summary>
    /// The length of generated game codes.
    /// </summary>
    internal static readonly int CODE_LENGTH = 6;

    /// <summary>
    /// Find lobby by gamecode
    /// </summary>
    /// <param name="gameCode"></param>
    internal static void SearchLobbyByGameCode(string gameCode)
    {
        Transitions.SetLoading();
        MelonLogger.Msg($"[NetLobby] Searching for lobby with code: {gameCode}");

        try
        {
            var lobbyQuery = SteamMatchmaking.LobbyList;
            lobbyQuery.maxResults = new Il2CppSystem.Nullable<int>(500);
            lobbyQuery.FilterDistanceWorldwide();
            lobbyQuery.slotsAvailable = new Il2CppSystem.Nullable<int>(1);
            lobbyQuery.WithKeyValue(ReplantedOnlineMod.Constants.GAME_CODE_KEY, gameCode);
            lobbyQuery.WithKeyValue(ReplantedOnlineMod.Constants.MOD_VERSION_KEY, Version_Check);
            lobbyQuery.ApplyFilters();

            lobbyQuery?.RequestAsync()?.ContinueWith((Action<Il2CppSystem.Threading.Tasks.Task<Il2CppStructArray<Lobby>>>)((task) =>
            {
                if (task.IsFaulted)
                {
                    MelonLogger.Error($"[NetLobby] Lobby search failed: {task.Exception}");
                    Transitions.ToMainMenu(() =>
                    {
                        ReplantedOnlinePopup.Show("Disconnected", $"An critical error occurred!");
                    });
                    return;
                }

                var lobbies = task.Result;

                if (lobbies == null)
                {
                    MelonLogger.Msg("[NetLobby] No lobbies found");
                    Transitions.ToMainMenu(() =>
                    {
                        ReplantedOnlinePopup.Show("Disconnected", $"Unable to find lobby with {gameCode} code!");
                    });
                    return;
                }

                MelonLogger.Msg($"[NetLobby] Found {lobbies.Length} lobbies matching filters");

                if (lobbies.Length > 0)
                {
                    var lobby = lobbies[0];

                    // Double-check the game code
                    string foundGameCode = lobby.GetData(ReplantedOnlineMod.Constants.GAME_CODE_KEY);

                    if (foundGameCode == gameCode)
                    {
                        // Verify mod version
                        string modVersion = lobby.GetData(ReplantedOnlineMod.Constants.MOD_VERSION_KEY);

                        if (modVersion != Version_Check)
                        {
                            MelonLogger.Warning($"[NetLobby] Mod version mismatch. Expected: {Version_Check}, Found: {modVersion}");
                            Transitions.ToMainMenu(() =>
                            {
                                ReplantedOnlinePopup.Show("Disconnected", $"Unable to join due to mod version mismatch\n{modVersion}");
                            });
                            return;
                        }

                        MelonLogger.Msg($"[NetLobby] Found matching lobby: {lobby.Id} with code {gameCode}");
                        NetLobby.JoinLobby(lobby.Id);
                    }
                    else
                    {
                        MelonLogger.Warning($"[NetLobby] Game code mismatch. Expected: {gameCode}, Found: {foundGameCode}");
                        Transitions.ToMainMenu(() =>
                        {
                            ReplantedOnlinePopup.Show("Disconnected", $"Unable to find lobby with {gameCode} code!");
                        });
                    }
                }
            }));
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[NetLobby] Error starting lobby search: {ex.Message}");
            Transitions.ToMainMenu();
        }
    }

    /// <summary>
    /// Retrieves a list of available multiplayer lobbies based on specified criteria.
    /// </summary>
    /// <param name="maxResults">The maximum number of lobbies to return in the search results.</param>
    /// <param name="callback">Callback method invoked with the array of found lobbies when the search completes successfully.</param>
    /// <param name="errorCallback">Optional callback method invoked when an error occurs or no lobbies are found.</param>
    internal static void GetLobbyList(int maxResults, Action<Lobby[]> callback, Action<LobbyListError> errorCallback = null)
    {
        Transitions.SetLoading();
        MelonLogger.Msg($"[NetLobby] Searching for lobbies");

        try
        {
            var lobbyQuery = SteamMatchmaking.LobbyList;
            lobbyQuery.maxResults = new Il2CppSystem.Nullable<int>(maxResults);
            lobbyQuery.FilterDistanceWorldwide();
            lobbyQuery.slotsAvailable = new Il2CppSystem.Nullable<int>(1);
            lobbyQuery.WithKeyValue(ReplantedOnlineMod.Constants.MOD_VERSION_KEY, ModInfo.MOD_VERSION);
            lobbyQuery.ApplyFilters();

            lobbyQuery?.RequestAsync()?.ContinueWith((Action<Il2CppSystem.Threading.Tasks.Task<Il2CppStructArray<Lobby>>>)((task) =>
            {
                if (task.IsFaulted)
                {
                    MelonLogger.Error($"[NetLobby] Lobby search failed: {task.Exception}");
                    errorCallback?.Invoke(LobbyListError.Error);
                    return;
                }

                var lobbies = task.Result;

                if (lobbies == null)
                {
                    MelonLogger.Msg("[NetLobby] No lobbies found");
                    errorCallback?.Invoke(LobbyListError.NoneFound);
                    return;
                }

                MelonLogger.Msg($"[NetLobby] Found {lobbies.Length} lobbies");

                callback(lobbies);
            }));
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"[NetLobby] Error starting lobby search: {ex.Message}");
            errorCallback?.Invoke(LobbyListError.Error);
        }
    }

    /// <summary>
    /// Sets the lobby data including version information and game code.
    /// </summary>
    /// <param name="data">The network lobby data containing the lobby ID.</param>
    internal static void SetLobbyData(NetLobbyData data)
    {
        SteamMatchmaking.Internal.SetLobbyData(data.LobbyId, ReplantedOnlineMod.Constants.MOD_VERSION_KEY, Version_Check);
        var gameCode = GenerateGameCode(data.LobbyId);
        SteamMatchmaking.Internal.SetLobbyData(data.LobbyId, ReplantedOnlineMod.Constants.GAME_CODE_KEY, gameCode);
        SteamMatchmaking.Internal.SetLobbyType(data.LobbyId, LobbyType.Public);
    }

    /// <summary>
    /// Sets whether the current lobby can be joined by other players.
    /// </summary>
    /// <param name="bool">True to allow players to join, false to make the lobby private/invite-only.</param>
    internal static void SetJoinable(bool @bool)
    {
        if (!NetLobby.AmInLobby()) return;

        SteamMatchmaking.Internal.SetLobbyJoinable(NetLobby.LobbyData.LobbyId, @bool);
    }

    /// <summary>
    /// Generates a consistent game code based on the lobby Steam ID
    /// </summary>
    private static string GenerateGameCode(SteamId lobbyId)
    {
        // Use the lobby ID as a seed for consistent code generation
        ulong seed = lobbyId;
        var random = new Random((int)(seed & 0xFFFFFFFF));

        StringBuilder codeBuilder = new();
        for (int i = 0; i < CODE_LENGTH; i++)
        {
            codeBuilder.Append(CODE_CHARS[random.Next(CODE_CHARS.Length)]);
        }

        string gameCode = codeBuilder.ToString();
        MelonLogger.Msg($"[NetLobby] Generated game code: {gameCode} for lobby {lobbyId}");
        return gameCode;
    }
}

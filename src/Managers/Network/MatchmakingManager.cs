using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSteamworks;
using Il2CppSteamworks.Data;
using ReplantedOnline.Enums.Modded;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Managers.Modded;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Modules.Reloaded.Panel;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Discord;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.MelonLoader;
using System.Net;
using System.Text;

namespace ReplantedOnline.Managers.Network;

/// <summary>
/// Manages Steam matchmaking functionality for finding and joining multiplayer lobbies in ReplantedOnline.
/// Handles lobby searching by game codes and generates consistent lobby identifiers.
/// </summary>
internal static class MatchmakingManager
{
    /// <summary>
    /// Character set used for generating game codes. Excludes confusing characters like O/0 and I/1.
    /// </summary>
    internal static readonly char[] CODE_CHARS = "ABCDEFHIJKLMNPQRSTUVWXYZ".ToCharArray();

    internal static readonly char[] CODE_REPLANTED_POSTFIX_CHARS = "BCDEFHIJKLM".ToCharArray();

    internal static readonly char[] CODE_SPACEWAR_POSTFIX_CHARS = "NPQRSTUVWXY".ToCharArray();

    /// <summary>
    /// The length of generated game codes.
    /// </summary>
    internal static readonly int CODE_LENGTH = 6;

    /// <summary>
    /// Find lobby by Game Code.
    /// </summary>
    /// <param name="gameCode"></param>
    internal static void SearchSteamLobbyByGameCode(string gameCode)
    {
        Transitions.SetLoading();
        ReplantedOnlineMod.Logger.Msg(typeof(MatchmakingManager), $"Searching for lobby with code: {gameCode}");

        try
        {
            var lobbyQuery = SteamMatchmaking.LobbyList;
            lobbyQuery.maxResults = new Il2CppSystem.Nullable<int>(500);
            lobbyQuery.FilterDistanceWorldwide();
            lobbyQuery.slotsAvailable = new Il2CppSystem.Nullable<int>(1);
            lobbyQuery.WithKeyValue(ReplantedOnlineMod.Constants.Network.GAME_CODE_KEY, gameCode);
            lobbyQuery.ApplyFilters();

            lobbyQuery?.RequestAsync()?.ContinueWith((Action<Il2CppSystem.Threading.Tasks.Task<Il2CppStructArray<Lobby>>>)((task) =>
            {
                if (task.IsFaulted)
                {
                    ReplantedOnlineMod.Logger.Error(typeof(MatchmakingManager), $"Lobby search failed: {task.Exception}");
                    Transitions.ToMainMenu(() =>
                    {
                        CustomPopupPanel.Show("Disconnected", $"An critical error occurred!");
                    });
                    return;
                }

                var lobbies = task.Result;

                if (lobbies == null)
                {
                    ReplantedOnlineMod.Logger.Msg(typeof(MatchmakingManager), "No lobbies found");
                    Transitions.ToMainMenu(() =>
                    {
                        CustomPopupPanel.Show("Disconnected", $"Unable to find lobby with {gameCode} code!");
                    });
                    return;
                }

                ReplantedOnlineMod.Logger.Msg(typeof(MatchmakingManager), $"Found {lobbies.Length} lobbies matching filters");

                if (lobbies.Length > 0)
                {
                    var lobby = lobbies[0];

                    // Double-check the game code
                    string foundGameCode = lobby.GetData(ReplantedOnlineMod.Constants.Network.GAME_CODE_KEY);

                    if (foundGameCode == gameCode)
                    {
                        // Verify mod version
                        string modVersion = lobby.GetData(ReplantedOnlineMod.Constants.Network.MOD_VERSION_KEY);

                        if (modVersion != ModInfo.MOD_VERSION_FORMATTED)
                        {
                            ReplantedOnlineMod.Logger.Warning(typeof(MatchmakingManager), $"Mod version mismatch. Expected: v{ModInfo.MOD_VERSION_FORMATTED}, Found: {modVersion}");
                            Transitions.ToMainMenu(() =>
                            {
                                CustomPopupPanel.Show("Disconnected", $"Unable to join due to mod version mismatch\nv{modVersion}");
                            });
                            return;
                        }

                        ReplantedOnlineMod.Logger.Msg(typeof(MatchmakingManager), $"Found matching lobby: {lobby.Id} with code {gameCode}");
                        ReloadedLobby.JoinLobby(lobby.Id);
                    }
                    else
                    {
                        ReplantedOnlineMod.Logger.Warning(typeof(MatchmakingManager), $"Game code mismatch. Expected: {gameCode}, Found: {foundGameCode}");
                        Transitions.ToMainMenu(() =>
                        {
                            CustomPopupPanel.Show("Disconnected", $"Unable to find lobby with {gameCode} code!");
                        });
                    }
                }
            }));
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(typeof(MatchmakingManager), $"Error starting lobby search: {ex.Message}");
            Transitions.ToMainMenu();
        }
    }

    /// <summary>
    /// Retrieves a list of available multiplayer lobbies based on specified criteria.
    /// </summary>
    /// <param name="maxResults">The maximum number of lobbies to return in the search results.</param>
    /// <param name="callback">Callback method invoked with the array of found lobbies when the search completes successfully.</param>
    /// <param name="errorCallback">Optional callback method invoked when an error occurs or no lobbies are found.</param>
    internal static void GetSteamLobbyList(int maxResults, Action<Lobby[]> callback, Action<LobbyListError> errorCallback = null)
    {
        Transitions.SetLoading();
        ReplantedOnlineMod.Logger.Msg(typeof(MatchmakingManager), $"Searching for lobbies");

        try
        {
            var lobbyQuery = SteamMatchmaking.LobbyList;
            lobbyQuery.maxResults = new Il2CppSystem.Nullable<int>(maxResults);
            lobbyQuery.FilterDistanceWorldwide();
            lobbyQuery.slotsAvailable = new Il2CppSystem.Nullable<int>(1);
            lobbyQuery.WithKeyValue(ReplantedOnlineMod.Constants.Network.MOD_VERSION_KEY, ModInfo.MOD_VERSION);
            lobbyQuery.ApplyFilters();

            lobbyQuery?.RequestAsync()?.ContinueWith((Action<Il2CppSystem.Threading.Tasks.Task<Il2CppStructArray<Lobby>>>)((task) =>
            {
                if (task.IsFaulted)
                {
                    ReplantedOnlineMod.Logger.Error(typeof(MatchmakingManager), $"Lobby search failed: {task.Exception}");
                    errorCallback?.Invoke(LobbyListError.Error);
                    return;
                }

                var lobbies = task.Result;

                if (lobbies == null)
                {
                    ReplantedOnlineMod.Logger.Msg(typeof(MatchmakingManager), "No lobbies found");
                    errorCallback?.Invoke(LobbyListError.NoneFound);
                    return;
                }

                ReplantedOnlineMod.Logger.Msg(typeof(MatchmakingManager), $"Found {lobbies.Length} lobbies");

                callback(lobbies);
            }));
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(typeof(MatchmakingManager), $"Error starting lobby search: {ex.Message}");
            errorCallback?.Invoke(LobbyListError.Error);
        }
    }

    /// <summary>
    /// Sets the lobby data including version information and game code.
    /// </summary>
    /// <param name="data">The network lobby data containing the lobby ID.</param>
    internal static void SetLobbyData(ReloadedLobbyData data)
    {
        ReloadedLobby.NetworkTransport.SetLobbyData(data.LobbyId, ReplantedOnlineMod.Constants.Network.MOD_VERSION_KEY, ModInfo.MOD_VERSION_FORMATTED);
        var gameCode = GenerateGameCode(data.LobbyId);
        ReloadedLobby.NetworkTransport.SetLobbyData(data.LobbyId, ReplantedOnlineMod.Constants.Network.GAME_CODE_KEY, gameCode);
        ReloadedLobby.NetworkTransport.SetLobbyType(data.LobbyId, LobbyType.Public);
    }

    /// <summary>
    /// Updates the joinable state of the current lobby, optionally overriding the value. By default, the lobby is joinable if the game has not started and not joinable if the game has started.
    /// </summary>
    internal static void UpdateLobbyJoinable(bool? @override = null)
    {
        if (!ReloadedLobby.AmInLobby()) return;

        if (@override != null)
        {
            ReloadedLobby.LobbyData.LobbyJoinable.Value = @override.Value;
            ReloadedLobby.NetworkTransport.SetLobbyJoinable(ReloadedLobby.LobbyData.LobbyId, @override.Value);
            DiscordManager.SetJoinable(@override.Value);
            return;
        }

        bool canJoin = !ReloadedLobby.LobbyData.HasStarted.Value;
        ReloadedLobby.LobbyData.LobbyJoinable.Value = canJoin;
        ReloadedLobby.NetworkTransport.SetLobbyJoinable(ReloadedLobby.LobbyData.LobbyId, canJoin);
        DiscordManager.SetJoinable(canJoin);
    }

    /// <summary>
    /// Generates a consistent game code based on the lobby ID
    /// </summary>
    internal static string GenerateGameCode(ID lobbyId)
    {
        // Extract a numeric seed from the ID
        ulong seed;

        if (lobbyId.TryGetSteamId(out SteamId steamId))
        {
            seed = steamId;
        }
        else if (lobbyId.TryGetULong(out ulong ulongId))
        {
            seed = ulongId;
        }
        else if (lobbyId.TryGetIPEndPoint(out IPEndPoint endpoint))
        {
            // Combine address bytes and port for a seed
            byte[] addressBytes = endpoint.Address.GetAddressBytes();
            seed = (ulong)endpoint.Port;
            for (int i = 0; i < Math.Min(4, addressBytes.Length); i++)
            {
                seed = seed << 8 | addressBytes[i];
            }
        }
        else
        {
            throw new InvalidOperationException($"Unsupported lobby ID type for generating game code: {lobbyId.UnderlyingType}");
        }

        var random = new Random((int)(seed & 0xFFFFFFFF));

        StringBuilder codeBuilder = new();
        for (int i = 0; i < CODE_LENGTH - 1; i++)
        {
            codeBuilder.Append(CODE_CHARS[random.Next(CODE_CHARS.Length)]);
        }
        codeBuilder.Append(GetGameCodePostfix(random));

        string gameCode = codeBuilder.ToString();
        ReplantedOnlineMod.Logger.Msg(typeof(MatchmakingManager), $"Generated game code: {gameCode} for lobby {lobbyId}");
        return gameCode;
    }

    /// <summary>
    /// Gets a random postfix character for the game code based on the current application configuration.
    /// </summary>
    /// <param name="random">The random number generator to use for selecting the postfix character.</param>
    /// <returns>A character that identifies the application type (Replanted or Spacewar).</returns>
    internal static char GetGameCodePostfix(Random random)
    {
        switch (BloomEngineManager.BloomConfigs.AppServerConfig.Value)
        {
            case AppIds.Replanted:
                return CODE_REPLANTED_POSTFIX_CHARS[random.Next(CODE_REPLANTED_POSTFIX_CHARS.Length)];
            case AppIds.Spacewar:
                return CODE_SPACEWAR_POSTFIX_CHARS[random.Next(CODE_SPACEWAR_POSTFIX_CHARS.Length)];
            default:
                return CODE_CHARS.Last();
        }
    }

    /// <summary>
    /// Determines the application type from a game code based on its postfix character.
    /// </summary>
    /// <param name="gameCode">The game code string to analyze.</param>
    /// <returns>The AppIds value corresponding to the game code's postfix, or 0 if no match is found.</returns>
    internal static AppIds GetGameCodePostfixType(string gameCode)
    {
        foreach (var character in CODE_REPLANTED_POSTFIX_CHARS)
        {
            if (gameCode.EndsWith(character))
            {
                return AppIds.Replanted;
            }
        }

        foreach (var character in CODE_SPACEWAR_POSTFIX_CHARS)
        {
            if (gameCode.EndsWith(character))
            {
                return AppIds.Spacewar;
            }
        }

        return 0;
    }
}

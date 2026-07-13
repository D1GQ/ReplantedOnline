using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSteamworks;
using Il2CppSteamworks.Data;
using ReplantedOnline.Enums.Modded;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Managers.Modded;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Modules.Reloaded.Panel;
using ReplantedOnline.Network.Discord;
using ReplantedOnline.Patches.Steam;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.Il2Cpp;
using ReplantedOnline.Utilities.MelonLoader;
using System.Net;
using System.Text;

namespace ReplantedOnline.Network.Reloaded.Client;

/// <summary>
/// Provides matchmaking functionality for ReplantedOnline.
/// </summary>
internal static class ReloadedMatchmaking
{
    /// <summary>
    /// Character set used for generating game codes. Excludes confusing characters like O/0 and I/1.
    /// </summary>
    internal static readonly char[] CODE_CHARS = "ABCDEFHIJKLMNPQRSTUVWXYZ".ToCharArray();

    /// <summary>
    /// Character set used for the postfix of game codes when the application is running as Replanted.
    /// </summary>
    internal static readonly char[] CODE_REPLANTED_POSTFIX_CHARS = "BCDEFHIJKLM".ToCharArray();

    /// <summary>
    /// Character set used for the postfix of game codes when the application is running as Spacewar.
    /// </summary>
    internal static readonly char[] CODE_SPACEWAR_POSTFIX_CHARS = "NPQRSTUVWXY".ToCharArray();

    /// <summary>
    /// The length of generated game codes (excluding the postfix character).
    /// </summary>
    internal static readonly int CODE_LENGTH = 6;

    /// <summary>
    /// Attempts to join a Steam lobby using the specified game code.
    /// </summary>
    /// <param name="gameCode">The game code to search for and join.</param>
    internal static void JoinSteamLobbyByGameCode(string gameCode)
    {
        SteamClientPatch.TrySetTempApp(GetGameCodePostfixType(gameCode));
        Transitions.SetLoading();

        try
        {
            FetchSteamLobbyList((result, lobbies) =>
            {
                switch (result)
                {
                    case LobbyListResult.Succeed:
                        {
                            var lobby = lobbies[0];

                            // Double-check the game code
                            string foundGameCode = lobby.GetData(ReplantedOnlineMod.Constants.Network.GAME_CODE_KEY);

                            if (foundGameCode == gameCode)
                            {
                                // Verify mod version
                                string modVersion = lobby.GetData(ReplantedOnlineMod.Constants.Network.MOD_VERSION_KEY);

                                if (modVersion != ReplantedOnlineMod.ModInfo.MOD_VERSION_FORMATTED)
                                {
                                    ReplantedOnlineMod.Logger.Warning(typeof(ReloadedMatchmaking), $"Mod version mismatch. Expected: v{ReplantedOnlineMod.ModInfo.MOD_VERSION_FORMATTED}, Found: {modVersion}");
                                    Transitions.ToMainMenu(() =>
                                    {
                                        CustomPopupPanel.Show("Disconnected", $"Unable to join due to mod version mismatch\nv{modVersion}");
                                    });
                                    return;
                                }

                                ReplantedOnlineMod.Logger.Msg(typeof(ReloadedMatchmaking), $"Found matching lobby: {lobby.Id} with code {gameCode}");
                                ReloadedLobby.JoinLobby(lobby.Id);
                            }
                            else
                            {
                                ReplantedOnlineMod.Logger.Warning(typeof(ReloadedMatchmaking), $"Game code mismatch. Expected: {gameCode}, Found: {foundGameCode}");
                                Transitions.ToMainMenu(() =>
                                {
                                    CustomPopupPanel.Show("Disconnected", $"Unable to find lobby with {gameCode} code!");
                                });
                            }
                        }
                        break;
                    case LobbyListResult.Failed:
                        Transitions.ToMainMenu(() =>
                        {
                            CustomPopupPanel.Show("Disconnected", $"Unable to find lobby with {gameCode} code!");
                        });
                        break;
                    case LobbyListResult.Error:
                        Transitions.ToMainMenu(() =>
                        {
                            CustomPopupPanel.Show("Disconnected", $"An critical error occurred!");
                        });
                        break;
                }
            }, 500, (ReplantedOnlineMod.Constants.Network.GAME_CODE_KEY, gameCode));
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(typeof(ReloadedMatchmaking), $"Error starting join Replanted Steam lobby by game code: {ex.Message}");
            Transitions.ToMainMenu();
        }
    }

    /// <summary>
    /// Fetches a list of Steam lobbies that match the specified filters.
    /// </summary>
    /// <param name="callback">The callback to invoke when the lobby search completes. Receives a result status and an array of matching lobbies.</param>
    /// <param name="maxResults">The maximum number of lobbies to return in the result.</param>
    /// <param name="filters">Key-value pairs to filter the lobby search. Each pair represents a lobby data key and its expected value.</param>
    internal static void FetchSteamLobbyList(Action<LobbyListResult, Lobby[]> callback, int maxResults, params (string Key, string Value)[] filters)
    {
        if (filters.Length > 0)
        {
            ReplantedOnlineMod.Logger.Msg(typeof(ReloadedMatchmaking), $"Fetching Replanted Steam lobbies with filters: {string.Join(", ", filters.Select(kvp => kvp.Value))}");
        }
        else
        {
            ReplantedOnlineMod.Logger.Msg(typeof(ReloadedMatchmaking), $"Fetching Replanted Steam lobbies");
        }

        try
        {
            var lobbyQuery = SteamMatchmaking.LobbyList;
            lobbyQuery.maxResults = new Il2CppSystem.Nullable<int>(maxResults);
            lobbyQuery.FilterDistanceWorldwide();
            lobbyQuery.slotsAvailable = new Il2CppSystem.Nullable<int>(1);
            lobbyQuery.WithKeyValue(ReplantedOnlineMod.Constants.Network.MOD_KEY, ReplantedOnlineMod.ModInfo.MOD_GUID);
            foreach (var (key, value) in filters)
            {
                lobbyQuery.WithKeyValue(key, value);
            }
            lobbyQuery.ApplyFilters();

            lobbyQuery?.RequestAsync()?.ContinueWith((Action<Il2CppSystem.Threading.Tasks.Task<Il2CppStructArray<Lobby>>>)((task) =>
            {
                if (task.IsFaulted)
                {
                    ReplantedOnlineMod.Logger.Error(typeof(ReloadedMatchmaking), $"Replanted Steam Lobby search failed: {task.Exception}");
                    callback.Invoke(LobbyListResult.Error, null!);
                    return;
                }

                if (task.Result == null)
                {
                    ReplantedOnlineMod.Logger.Msg(typeof(ReloadedMatchmaking), "No Replanted Steam lobbies found");
                    callback.Invoke(LobbyListResult.Failed, []);
                    return;
                }

                ReplantedOnlineMod.Logger.Msg(typeof(ReloadedMatchmaking), $"Found {task.Result.Length} Replanted Steam lobbies");
                callback.Invoke(LobbyListResult.Succeed, task.Result.ToArray().ToManagedArray());
            }));
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(typeof(ReloadedMatchmaking), $"Error starting Replanted Steam lobby search: {ex.Message}");
            callback.Invoke(LobbyListResult.Error, null!);
        }
    }

    /// <summary>
    /// Sets the lobby data.
    /// </summary>
    /// <param name="data">The network lobby data containing the lobby ID.</param>
    internal static void SetLobbyData(ReloadedLobbyData data)
    {
        ReloadedLobby.NetworkTransport!.SetLobbyData(data.LobbyId, ReplantedOnlineMod.Constants.Network.MOD_KEY, ReplantedOnlineMod.ModInfo.MOD_GUID);
        ReloadedLobby.NetworkTransport.SetLobbyData(data.LobbyId, ReplantedOnlineMod.Constants.Network.MOD_VERSION_KEY, ReplantedOnlineMod.ModInfo.MOD_VERSION_FORMATTED);
        var gameCode = GenerateGameCode(data.LobbyId);
        ReloadedLobby.NetworkTransport.SetLobbyData(data.LobbyId, ReplantedOnlineMod.Constants.Network.GAME_CODE_KEY, gameCode);
        ReloadedLobby.NetworkTransport.SetLobbyType(data.LobbyId, LobbyType.Public);
    }

    /// <summary>
    /// Updates the joinable state of the current lobby.
    /// </summary>
    /// <param name="override">Optional override value for the joinable state. If not provided, the joinable state is automatically determined based on whether the game has started.</param>
    internal static void UpdateLobbyJoinable(bool? @override = null)
    {
        if (!ReloadedLobby.AmInLobby()) return;

        if (@override != null)
        {
            ReloadedLobby.LobbyData!.LobbyJoinable.Value = @override.Value;
            ReloadedLobby.NetworkTransport!.SetLobbyJoinable(ReloadedLobby.LobbyData.LobbyId, @override.Value);
            DiscordManager.SetJoinable(@override.Value);
            return;
        }

        bool canJoin = !ReloadedLobby.LobbyData!.HasStarted.Value;
        ReloadedLobby.LobbyData.LobbyJoinable.Value = canJoin;
        ReloadedLobby.NetworkTransport!.SetLobbyJoinable(ReloadedLobby.LobbyData.LobbyId, canJoin);
        DiscordManager.SetJoinable(canJoin);
    }

    /// <summary>
    /// Generates a consistent game code based on the lobby ID.
    /// </summary>
    /// <param name="lobbyId">The lobby ID to generate a game code for.</param>
    /// <returns>A 6-character game code consisting of letters, with the last character identifying the application type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the lobby ID type is not supported.</exception>
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
        ReplantedOnlineMod.Logger.Msg(typeof(ReloadedMatchmaking), $"Generated game code: {gameCode} for lobby {lobbyId}");
        return gameCode;
    }

    /// <summary>
    /// Gets a random postfix character for the game code based on the current application configuration.
    /// </summary>
    /// <param name="random">The random number generator to use for selecting the postfix character.</param>
    /// <returns>A character that identifies the application type (Replanted or Spacewar).</returns>
    private static char GetGameCodePostfix(Random random)
    {
        return BloomEngineManager.BloomConfigs.AppServerConfig.Value switch
        {
            AppIds.Replanted => CODE_REPLANTED_POSTFIX_CHARS[random.Next(CODE_REPLANTED_POSTFIX_CHARS.Length)],
            AppIds.Spacewar => CODE_SPACEWAR_POSTFIX_CHARS[random.Next(CODE_SPACEWAR_POSTFIX_CHARS.Length)],
            _ => CODE_CHARS.Last(),
        };
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
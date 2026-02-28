using Il2CppSteamworks;
using ReplantedOnline.Network.Steam;

namespace ReplantedOnline.Helper;

/// <summary>
/// Provides extension methods for Steamworks types to simplify common operations
/// and improve code readability throughout the ReplantedOnline mod.
/// </summary>
internal static class SteamNetExtensions
{
    /// <summary>
    /// Retrieves a SteamNetClient instance by Steam ID from the current lobby.
    /// </summary>
    /// <param name="steamId">The Steam ID to search for.</param>
    /// <returns>The SteamNetClient instance if found in the current lobby, otherwise null.</returns>
    internal static SteamNetClient GetNetClient(this SteamId steamId)
    {
        if (NetLobby.LobbyData?.AllClients.TryGetValue(steamId, out var client) == true)
        {
            return client;
        }

        return default;
    }
}
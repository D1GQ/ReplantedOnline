using Il2CppSteamworks;
using ReplantedOnline.Enums;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Structs;
using System.Net;

namespace ReplantedOnline.Helper;

/// <summary>
/// Provides extension methods for network-related classes in ReplantedOnline.
/// </summary>
internal static class NetExtensions
{
    /// <summary>
    /// Retrieves a NetClient instance by Client ID from the current lobby.
    /// </summary>
    /// <param name="clientId">The Client ID to search for.</param>
    /// <returns>The NetClient instance if found in the current lobby, otherwise null.</returns>
    internal static NetClient GetNetClient(this ID clientId)
    {
        if (NetLobby.LobbyData?.AllClients.TryGetValue(clientId, out var client) == true)
        {
            return client;
        }

        return default;
    }

    /// <summary>
    /// Creates a new ID instance that represents the specified Steam ID.
    /// </summary>
    /// <param name="steamId">The SteamId value to be converted into an ID.</param>
    /// <returns>An ID object corresponding to the provided SteamId.</returns>
    internal static ID AsID(this SteamId steamId) => new(steamId, IdType.SteamId);

    /// <summary>
    /// Creates a new <see cref="ID"/> instance representing the specified unsigned integer value.
    /// </summary>
    /// <param name="id">The unsigned integer value to be encapsulated in the <see cref="ID"/>.</param>
    /// <returns>An <see cref="ID"/> that represents the provided unsigned integer value.</returns>
    internal static ID AsID(this ulong id) => new(id, IdType.ULong);

    /// <summary>
    /// Creates an ID instance that uniquely represents the specified IP endpoint.
    /// </summary>
    /// <param name="endpoint">The IP endpoint to be represented by the ID. Cannot be null.</param>
    /// <returns>An ID object corresponding to the given IP endpoint.</returns>
    internal static ID AsID(this IPEndPoint endpoint) => new(endpoint, IdType.IPEndPoint);
}
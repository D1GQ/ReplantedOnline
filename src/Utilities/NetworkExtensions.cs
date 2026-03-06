using Il2CppReloaded.Gameplay;
using Il2CppSteamworks;
using ReplantedOnline.Enums;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Object;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Structs;
using System.Net;

namespace ReplantedOnline.Utilities;

/// <summary>
/// Provides helper methods for managing network object lookups and associations.
/// </summary>
internal static class NetworkExtensions
{
    /// <summary>
    /// Dictionary storing networked lookups by type and object instance.
    /// </summary>
    internal static Dictionary<Type, Dictionary<object, NetworkObject>> NetworkedLookups = [];

    /// <summary>
    /// Associates a network object with an object instance for later retrieval.
    /// </summary>
    /// <param name="child">The object instance to associate with the network object.</param>
    /// <param name="networkObj">The network object to associate with the object.</param>
    internal static void AddNetworkedLookup(this object child, NetworkObject networkObj)
    {
        if (!NetworkedLookups.TryGetValue(child.GetType(), out var lookup))
        {
            lookup = NetworkedLookups[child.GetType()] = [];
        }
        lookup[child] = networkObj;
    }

    /// <summary>
    /// Removes the network object association for the specified object instance.
    /// </summary>
    /// <param name="child">The object instance to remove from network lookups.</param>
    internal static void RemoveNetworkedLookup(this object child)
    {
        if (NetworkedLookups.TryGetValue(child.GetType(), out var lookup))
        {
            lookup.Remove(child);

            if (lookup.Count == 0)
            {
                NetworkedLookups.Remove(child.GetType());
            }
        }
    }

    /// <summary>
    /// Retrieves the network object associated with the specified object instance.
    /// </summary>
    /// <typeparam name="T">The type of NetworkObject to retrieve.</typeparam>
    /// <param name="child">The object instance to look up.</param>
    /// <returns>The associated NetworkObject instance, or null if not found.</returns>
    internal static T GetNetworked<T>(this object child) where T : NetworkObject
    {
        if (NetworkedLookups.TryGetValue(child.GetType(), out var lookup))
        {
            if (lookup.TryGetValue(child, out var networkObj))
            {
                return (T)networkObj;
            }
        }

        return null;
    }

    /// <summary>
    /// Retrieves the network zombie associated with the specified zombie instance.
    /// </summary>
    /// <param name="zombie">The zombie instance to look up.</param>
    /// <returns>The associated ZombieNetworked instance, or null if not found.</returns>
    internal static ZombieNetworked GetZombieNetworked(this Zombie zombie)
    {
        return zombie.GetNetworked<ZombieNetworked>();
    }

    /// <summary>
    /// Retrieves the network plant associated with the specified plant instance.
    /// </summary>
    /// <param name="plant">The plant instance to look up.</param>
    /// <returns>The associated PlantNetworked instance, or null if not found.</returns>
    internal static PlantNetworked GetPlantNetworked(this Plant plant)
    {
        return plant.GetNetworked<PlantNetworked>();
    }

    /// <summary>
    /// Checks if the object has a network look up.
    /// </summary>
    /// <param name="child">The object instance to look up.</param>
    internal static bool HasNetworked(this object child)
    {
        if (NetworkedLookups.TryGetValue(child.GetType(), out var lookup))
        {
            return lookup.ContainsKey(child);
        }

        return false;
    }

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
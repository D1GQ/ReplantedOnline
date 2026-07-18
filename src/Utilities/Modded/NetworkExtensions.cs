using Il2CppReloaded.Gameplay;
using Il2CppSteamworks;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Network.Reloaded.Client.Object;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay;
using ReplantedOnline.Structs.Network;
using System.Net;

namespace ReplantedOnline.Utilities.Modded;

/// <summary>
/// Provides helper methods for managing network object lookups and associations.
/// </summary>
internal static class NetworkExtensions
{
    /// <summary>
    /// Lock object for thread-safe access to NetworkedLookups dictionary.
    /// </summary>
    private static readonly object _lock = new();

    /// <summary>
    /// Dictionary storing networked lookups.
    /// </summary>
    internal static readonly Dictionary<ReloadedObject, NetworkObject> NetworkedLookups = [];

    /// <summary>
    /// Associates a network object with an reloaded object instance for later retrieval.
    /// </summary>
    /// <param name="parent">The reloaded object instance to associate with the network object.</param>
    /// <param name="networkObj">The network object to associate with the reloaded object.</param>
    internal static void AddNetworkedLookup(this ReloadedObject parent, NetworkObject networkObj)
    {
        if (parent == null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        if (networkObj == null)
        {
            throw new ArgumentNullException(nameof(networkObj));
        }

        lock (_lock)
        {
            NetworkedLookups[parent] = networkObj;
        }
    }

    /// <summary>
    /// Removes the network object association for the specified reloaded objectinstance.
    /// </summary>
    /// <param name="parent">The reloaded object instance to remove from network lookups.</param>
    internal static void RemoveNetworkedLookup(this ReloadedObject parent)
    {
        if (parent == null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        lock (_lock)
        {
            NetworkedLookups.Remove(parent);
        }
    }

    /// <summary>
    /// Removes the network object association for the specified network object instance.
    /// </summary>
    /// <param name="child">The child network object instance to remove from network lookups.</param>
    internal static void RemoveNetworkedLookup(this NetworkObject child)
    {
        if (child == null)
        {
            throw new ArgumentNullException(nameof(child));
        }

        lock (_lock)
        {
            var childrenToRemove = NetworkedLookups
                .Where(x => x.Value == child)
                .Select(x => x.Key)
                .ToList();

            foreach (var c in childrenToRemove)
            {
                NetworkedLookups.Remove(c);
            }
        }
    }

    /// <summary>
    /// Retrieves the network object associated with the specified reloaded object instance.
    /// </summary>
    /// <typeparam name="T">The type of NetworkObject to retrieve.</typeparam>
    /// <param name="parent">The reloaded object instance to look up.</param>
    /// <returns>The associated NetworkObject instance, or null if not found.</returns>
    internal static T? GetNetworked<T>(this ReloadedObject parent) where T : NetworkObject
    {
        if (parent == null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        lock (_lock)
        {
            if (NetworkedLookups.TryGetValue(parent, out var networkObj))
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
    internal static ZombieNetworked? GetNetworked(this Zombie zombie)
    {
        return zombie.GetNetworked<ZombieNetworked>();
    }

    /// <summary>
    /// Retrieves the network plant associated with the specified plant instance.
    /// </summary>
    /// <param name="plant">The plant instance to look up.</param>
    /// <returns>The associated PlantNetworked instance, or null if not found.</returns>
    internal static PlantNetworked? GetNetworked(this Plant plant)
    {
        return plant.GetNetworked<PlantNetworked>();
    }

    /// <summary>
    /// Attempts to retrieve the network object associated with the specified reloaded object instance.
    /// </summary>
    /// <typeparam name="T">The type of NetworkObject to retrieve. Must derive from NetworkObject.</typeparam>
    /// <param name="parent">The reloaded object instance to look up.</param>
    /// <param name="networkObject">When this method returns, contains the associated NetworkObject instance if found; otherwise, null.</param>
    /// <returns>true if a network object was successfully retrieved for the specified reloaded object; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parent"/> is null.</exception>
    internal static bool TryGetNetworked<T>(this ReloadedObject parent, out T networkObject) where T : NetworkObject
    {
        if (parent == null)
        {
            throw new ArgumentNullException(nameof(parent));
        }

        lock (_lock)
        {
            if (NetworkedLookups.TryGetValue(parent, out var networkObj))
            {
                networkObject = (T)networkObj;
                return true;
            }
        }

        networkObject = default!;
        return false;
    }

    /// <summary>
    /// Checks if the reloaded object has a network look up.
    /// </summary>
    /// <param name="parent">The reloaded object instance to look up.</param>
    internal static bool HasNetworked(this ReloadedObject parent)
    {
        if (parent == null)
        {
            return false;
        }

        lock (_lock)
        {
            return NetworkedLookups.ContainsKey(parent);
        }
    }

    /// <summary>
    /// Retrieves a ReloadedClientData instance by Client ID from the current lobby.
    /// </summary>
    /// <param name="clientId">The Client ID to search for.</param>
    /// <returns>The ReloadedClientData instance if found in the current lobby, otherwise null.</returns>
    internal static ReloadedClientData? GetClientData(this ID clientId)
    {
        if (ReloadedLobby.LobbyData?.AllClients.TryGetValue(clientId, out var client) == true)
        {
            return client;
        }

        return null;
    }

    /// <summary>
    /// Attempts to retrieve a ReloadedClientData instance by Client ID from the current lobby.
    /// </summary>
    /// <param name="clientId">The Client ID to search for.</param>
    /// <param name="client">When this method returns, contains the ReloadedClientData instance if found; otherwise, the default value.</param>
    /// <returns>true if the ReloadedClientData was found in the current lobby; otherwise, false.</returns>
    internal static bool TryGetClientData(this ID clientId, out ReloadedClientData client)
    {
        if (ReloadedLobby.LobbyData?.AllClients.TryGetValue(clientId, out var clientData) == true)
        {
            client = clientData;
            return true;
        }

        client = default!;
        return false;
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
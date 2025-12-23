using ReplantedOnline.Network.Object;

namespace ReplantedOnline.Helper;

/// <summary>
/// Provides helper methods for managing network object lookups and associations.
/// </summary>
internal static class NetworkHelper
{
    /// <summary>
    /// Dictionary storing networked lookups by type and object instance.
    /// </summary>
    internal static Dictionary<Type, Dictionary<object, NetworkClass>> NetworkedLookups = [];

    /// <summary>
    /// Associates a network class with an object instance for later retrieval.
    /// </summary>
    /// <param name="child">The object instance to associate with the network class.</param>
    /// <param name="networkClass">The network class to associate with the object.</param>
    internal static void AddNetworkedLookup(this object child, NetworkClass networkClass)
    {
        if (!NetworkedLookups.TryGetValue(child.GetType(), out var lookup))
        {
            lookup = NetworkedLookups[child.GetType()] = [];
        }
        lookup[child] = networkClass;
    }

    /// <summary>
    /// Removes the network class association for the specified object instance.
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
    /// Retrieves the network class associated with the specified object instance.
    /// </summary>
    /// <typeparam name="T">The type of NetworkClass to retrieve.</typeparam>
    /// <param name="child">The object instance to look up.</param>
    /// <returns>The associated NetworkClass instance, or null if not found.</returns>
    internal static T GetNetworked<T>(this object child) where T : NetworkClass
    {
        if (NetworkedLookups.TryGetValue(child.GetType(), out var lookup))
        {
            if (lookup.TryGetValue(child, out var networkClass))
            {
                return (T)networkClass;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if the object has a network look up.
    /// </summary>
    /// <typeparam name="T">The type of NetworkClass to retrieve.</typeparam>
    /// <param name="child">The object instance to look up.</param>
    internal static bool HasNetworked(this object child)
    {
        if (NetworkedLookups.TryGetValue(child.GetType(), out var lookup))
        {
            return lookup.ContainsKey(child);
        }

        return false;
    }
}
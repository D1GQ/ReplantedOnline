using UnityEngine;

namespace ReplantedOnline.Modules;

/// <summary>
/// Provides static container GameObjects.
/// </summary>
internal static class GlobalGameObjects
{
    /// <summary>
    /// Gets the container GameObject for all prefabs. Creates a new "Prefabs" GameObject if none exists, and marks it to persist across scene loads.
    /// </summary>
    /// <value>The GameObject that serves as the container for prefabs.</value>
    internal static GameObject PrefabsGo
    {
        get
        {
            if (field == null)
            {
                field = new GameObject("Prefabs");
                UnityEngine.Object.DontDestroyOnLoad(field);
            }

            return field;
        }
    }

    /// <summary>
    /// Gets the container GameObject for all network prefabs. Creates a new "NetworkPrefabs" GameObject if none exists, and marks it to persist across scene loads.
    /// </summary>
    /// <value>The GameObject that serves as the container for network prefabs.</value>
    internal static GameObject NetworkPrefabsObj
    {
        get
        {
            if (field == null)
            {
                field = new GameObject("NetworkPrefabs");
                UnityEngine.Object.DontDestroyOnLoad(field);
            }

            return field;
        }
    }

    /// <summary>
    /// Gets the container GameObject for all network objects. Creates a new "NetworkObjects" GameObject if none exists.
    /// </summary>
    /// <value>The GameObject that serves as the container for network objects.</value>
    internal static GameObject NetworkObjectsGo
    {
        get
        {
            if (field == null)
            {
                field = new GameObject("NetworkObjects");
            }

            return field;
        }
    }
}
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace ReplantedOnline.Monos;

/// <summary>
/// Abstract base class for creating and managing runtime prefabs in Unity.
/// Provides functionality for creating, cloning, and tracking prefabs at runtime.
/// </summary>
internal abstract class RuntimePrefab : MonoBehaviour
{
    /// <summary>
    /// Dictionary storing all registered runtime prefabs by their GUID.
    /// </summary>
    internal static readonly Dictionary<string, RuntimePrefab> Prefabs = [];

    private static GameObject _prefabsGo;

    /// <summary>
    /// Gets the root GameObject that contains all runtime prefabs.
    /// Creates the object if it doesn't exist.
    /// </summary>
    /// <value>The parent GameObject for all runtime prefabs.</value>
    internal static GameObject PrefabsGo
    {
        get
        {
            if (_prefabsGo == null)
            {
                _prefabsGo = new GameObject("Prefabs");
                DontDestroyOnLoad(_prefabsGo);
            }
            return _prefabsGo;
        }
    }

    /// <summary>
    /// Indicates whether this instance is a prefab template or a live clone in the scene.
    /// </summary>
    internal bool IsPrefab = true;

    /// <summary>
    /// The unique identifier for this prefab instance.
    /// </summary>
    internal string GUID { get; private set; }

    /// <summary>
    /// Gets the current state of the object.
    /// This property can hold any type of object to represent various states.
    /// </summary>
    internal object _State { get; set; }

    /// <summary>
    /// Creates a new runtime prefab of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of RuntimePrefab to create, must inherit from RuntimePrefab.</typeparam>
    /// <param name="prefabGUID">The unique identifier for the prefab.</param>
    /// <param name="callback">Optional callback to configure the prefab after creation.</param>
    /// <returns>The created prefab instance.</returns>
    internal static T CreatePrefab<T>(string prefabGUID, Action<T> callback = null) where T : RuntimePrefab
    {
        var go = new GameObject($"{typeof(T).Name}_Prefab");
        go.transform.SetParent(PrefabsGo.transform);
        go.SetActive(false);
        var prefab = go.AddComponent<T>();
        prefab.GUID = prefabGUID;
        callback?.Invoke(prefab);
        Prefabs[prefabGUID] = prefab;
        return prefab;
    }

    /// <summary>
    /// Clones a runtime prefab by its GUID.
    /// </summary>
    /// <param name="prefabGUID">The GUID of the prefab to clone.</param>
    /// <returns>A new instance of the cloned prefab, or null if the GUID is not found.</returns>
    internal static RuntimePrefab Clone(string prefabGUID)
    {
        if (Prefabs.TryGetValue(prefabGUID, out var prefab))
        {
            RuntimePrefab @new = Instantiate(prefab);
            @new.IsPrefab = false;
            @new.GUID = prefab.GUID;
            @new.OnClone(prefab);
            return @new;
        }

        return null;
    }

    /// <summary>
    /// Clones a runtime prefab by its GUID and returns it as the specified type.
    /// </summary>
    /// <typeparam name="T">The expected type of the prefab, must inherit from RuntimePrefab.</typeparam>
    /// <param name="prefabGUID">The GUID of the prefab to clone.</param>
    /// <returns>A new instance of the cloned prefab cast to type T, or null if not found.</returns>
    internal static T Clone<T>(string prefabGUID) where T : RuntimePrefab => Clone(prefabGUID) as T;

    /// <summary>
    /// Clones this specific prefab instance using its stored GUID.
    /// </summary>
    /// <returns>A new instance of this prefab.</returns>
    [HideFromIl2Cpp]
    internal RuntimePrefab Clone() => Clone(GUID);

    /// <summary>
    /// Clones this specific prefab instance using its stored GUID and returns it as the specified type.
    /// </summary>
    /// <typeparam name="T">The expected type of the prefab, must inherit from RuntimePrefab.</typeparam>
    /// <returns>A new instance of this prefab cast to type T.</returns>
    [HideFromIl2Cpp]
    internal T Clone<T>() where T : RuntimePrefab => Clone<T>(GUID);

    /// <summary>
    /// Called when a prefab is cloned. Override this method to perform additional setup on cloned instances.
    /// </summary>
    /// <param name="prefab">The original prefab that was cloned.</param>
    [HideFromIl2Cpp]
    protected virtual void OnClone(RuntimePrefab prefab) { }
}
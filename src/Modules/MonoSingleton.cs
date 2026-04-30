using UnityEngine;

namespace ReplantedOnline.Modules;

/// <summary>
/// Static utility class for managing a singleton MonoBehaviour instance of type <typeparamref name="T"/>.
/// Provides methods to create and access a single, persistent instance.
/// </summary>
/// <typeparam name="T">The type of MonoBehaviour that will be managed as a singleton. Must have a parameterless constructor.</typeparam>
internal static class MonoSingleton<T> where T : MonoBehaviour, new()
{
    /// <summary>
    /// Gets the singleton instance of type <typeparamref name="T"/>.
    /// Returns <c>null</c> if <see cref="CreateInstance"/> has not been called yet.
    /// </summary>
    internal static T Instance { get; private set; }

    /// <summary>
    /// Creates a new singleton instance of type <typeparamref name="T"/> by instantiating a new GameObject.
    /// Does nothing and returns <c>null</c> if an instance already exists.
    /// </summary>
    /// <returns>The created singleton instance, or <c>null</c> if one already exists.</returns>
    internal static T CreateInstance()
    {
        if (Instance != null)
        {
            return null;
        }

        var go = new GameObject(typeof(T).Name + "Singleton");
        UnityEngine.Object.DontDestroyOnLoad(go);
        var instance = go.AddComponent<T>();
        Instance = instance;
        return instance;
    }
}
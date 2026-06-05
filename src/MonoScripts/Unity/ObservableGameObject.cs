using Il2CppInterop.Runtime.Attributes;
using MelonLoader;
using UnityEngine;

namespace ReplantedOnline.MonoScripts.Unity;

/// <summary>
/// Provides an observable wrapper for GameObject events.
/// </summary>
[RegisterTypeInIl2Cpp]
internal sealed class ObservableGameObject : MonoBehaviour
{
    /// <summary>
    /// Event that is invoked when the GameObject is destroyed.
    /// The parameter is the GameObject that is being destroyed.
    /// </summary>
    [HideFromIl2Cpp]
    internal event Action<GameObject>? OnGameObjectDestroy;

    private void OnDestroy()
    {
        OnGameObjectDestroy?.Invoke(gameObject);
    }
}
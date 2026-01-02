using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace ReplantedOnline.Monos;

/// <summary>
/// Provides an observable wrapper for GameObject events.
/// </summary>
internal sealed class ObservableGameObject : MonoBehaviour
{
    /// <summary>
    /// Event that is invoked when the GameObject is destroyed.
    /// The parameter is the GameObject that is being destroyed.
    /// </summary>
    [HideFromIl2Cpp]
    internal event Action<GameObject> OnGameObjectDestroy;

    public void OnDestroy()
    {
        OnGameObjectDestroy?.Invoke(gameObject);
    }
}
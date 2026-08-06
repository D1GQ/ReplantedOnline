using Il2CppInterop.Runtime.Attributes;
using MelonLoader;
using ReplantedOnline.Utilities.Unity;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.MonoScripts.Modded;

/// <summary>
/// Manages coroutines.
/// </summary>
[RegisterTypeInIl2Cpp]
internal sealed class CoroutineManager : MonoBehaviour
{
    /// <summary>
    /// Gets a singleton instance a MonoBehaviour for managing coroutines.
    /// </summary>
    /// <value>The MonoBehaviour instance used for managing coroutines.</value>
    internal static CoroutineManager Instance
    {
        get
        {
            if (field == null)
            {
                var go = new GameObject("CoroutineManager");
                field = go.AddComponent<CoroutineManager>();
            }

            return field;
        }
    }

    /// <summary>
    /// Starts a coroutine using the provided enumerator.
    /// </summary>
    /// <param name="enumerator">The IEnumerator that defines the coroutine's execution logic.</param>
    /// <returns>A Coroutine reference that can be used to control the coroutine's execution.</returns>
    [HideFromIl2Cpp]
    internal Coroutine StartCoroutine(IEnumerator enumerator)
    {
        return CoroutineUtils.StartCoroutine(this, enumerator);
    }
}
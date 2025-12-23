using Il2CppReloaded.Gameplay;
using Il2CppSource.UI;
using Il2CppTekly.DataModels.Binders;
using Il2CppTekly.Localizations;
using UnityEngine;

namespace ReplantedOnline.Helper;

/// <summary>
/// Provides helper methods for GameObject manipulation and cleanup.
/// </summary>
internal static class ObjectHelper
{
    /// <summary>
    /// Creates and returns a new instance of a disposable <see cref="ReloadedObject"/> of type <typeparamref name="T"/>.
    /// This method uses <see cref="Activator.CreateInstance{T}"/> to dynamically instantiate objects at runtime.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="ReloadedObject"/> to create. Must have a parameterless constructor.</typeparam>
    /// <returns>A new instance of type <typeparamref name="T"/>.</returns>
    internal static T CreateReloadedObject<T>() where T : ReloadedObject
    {
        return Activator.CreateInstance<T>();
    }

    /// <summary>
    /// Destroys all TextLocalizer components on the GameObject and its children.
    /// This is useful when replacing UI elements that have localization bindings that need to be cleaned up.
    /// </summary>
    /// <param name="go">The GameObject to search for TextLocalizer components.</param>
    internal static void DestroyAllTextLocalizers(this GameObject go)
    {
        foreach (var comp in go.GetComponentsInChildren<TextLocalizer>(true))
        {
            UnityEngine.Object.Destroy(comp);
        }
    }

    /// <summary>
    /// Destroys all ImageLocalizer components on the GameObject and its children.
    /// This is useful when replacing UI images that have localization bindings that need to be cleaned up.
    /// </summary>
    /// <param name="go">The GameObject to search for TextLocalizer components.</param>
    internal static void DestroyAllImageLocalizers(this GameObject go)
    {
        foreach (var comp in go.GetComponentsInChildren<ImageLocalizer>(true))
        {
            UnityEngine.Object.Destroy(comp);
        }
    }

    /// <summary>
    /// Destroys all DataModel binder components on the GameObject and its children.
    /// This includes VisibilityBinder, StringBinder, ButtonBinder, and InputBinder components.
    /// </summary>
    /// <param name="go">The GameObject to search for binder components.</param>
    internal static void DestroyAllBinders(this GameObject go)
    {
        foreach (var comp in go.GetComponentsInChildren<VisibilityBinder>(true))
        {
            UnityEngine.Object.Destroy(comp);
        }

        foreach (var comp in go.GetComponentsInChildren<StringBinder>(true))
        {
            UnityEngine.Object.Destroy(comp);
        }

        foreach (var comp in go.GetComponentsInChildren<ButtonBinder>(true))
        {
            UnityEngine.Object.Destroy(comp);
        }

        foreach (var comp in go.GetComponentsInChildren<InputBinder>(true))
        {
            UnityEngine.Object.Destroy(comp);
        }
    }
}
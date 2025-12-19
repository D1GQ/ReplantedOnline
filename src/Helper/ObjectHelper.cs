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
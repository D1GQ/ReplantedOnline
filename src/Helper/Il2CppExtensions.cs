using UnityEngine.Events;

namespace ReplantedOnline.Helper;

/// <summary>
/// Provides extension methods for Il2Cpp types to improve interoperability with C# and simplify common operations.
/// </summary>
internal static class Il2CppExtensions
{
    /// <summary>
    /// Adds a C# Action as a listener to a UnityEvent, simplifying Il2Cpp event subscription.
    /// </summary>
    /// <param name="unityEvent">The UnityEvent to add the listener to.</param>
    /// <param name="action">The Action to be invoked when the event is triggered.</param>
    /// <example>
    /// <code>
    /// button.onClick.AddListener(() => MelonLogger.Msg("Button clicked!"));
    /// </code>
    /// </example>
    internal static void AddListener(this UnityEvent unityEvent, Action action)
    {
        unityEvent.AddListener(action);
    }
}
namespace ReplantedOnline.Modules;

/// <summary>
/// Provides a simple wrapper for managing singleton instances of classes.
/// Useful for managing global mod components and ensuring single initialization.
/// </summary>
/// <typeparam name="T">The type of class to wrap as a singleton instance.</typeparam>
internal sealed class InstanceWrapper<T> where T : class
{
    /// <summary>
    /// Gets or sets the singleton instance of type T.
    /// </summary>
    internal static T Instance { get; set; } = default;
}
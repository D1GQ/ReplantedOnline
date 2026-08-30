namespace ReplantedOnline.Modules.Modded.Instance;

/// <summary>
/// Provides a wrapper for managing a singleton instance of type <typeparamref name="T"/>.
/// Useful for managing global mod components and ensuring single initialization across the application.
/// </summary>
/// <typeparam name="T">The type of class to wrap as a singleton instance. Must be a reference type.</typeparam>
internal sealed class GlobalInstance<T> where T : class
{
    /// <summary>
    /// Gets or sets the singleton instance of type <typeparamref name="T"/>.
    /// </summary>
    internal static T Instance { get; set; } = default!;
}
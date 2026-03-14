namespace ReplantedOnline.Utilities;

/// <summary>
/// Provides utility methods for working with enumerations.
/// </summary>
internal static class EnumUtils
{
    /// <summary>
    /// Gets the next enum value in the sequence, wrapping around to the first value when at the end.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="current">The current enum value.</param>
    /// <returns>The next enum value in the sequence, or the first value if current is the last.</returns>
    /// <exception cref="ArgumentException">Thrown if the type parameter is not an enum.</exception>
    internal static T Next<T>(this T current) where T : Enum
    {
        T[] values = [.. Enum.GetValues(typeof(T)).Cast<T>()];
        int currentIndex = Array.IndexOf(values, current);
        int nextIndex = (currentIndex + 1) % values.Length;
        return values[nextIndex];
    }

    /// <summary>
    /// Gets the previous enum value in the sequence, wrapping around to the last value when at the beginning.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="current">The current enum value.</param>
    /// <returns>The previous enum value in the sequence, or the last value if current is the first.</returns>
    /// <exception cref="ArgumentException">Thrown if the type parameter is not an enum.</exception>
    internal static T Previous<T>(this T current) where T : Enum
    {
        T[] values = [.. Enum.GetValues(typeof(T)).Cast<T>()];
        int currentIndex = Array.IndexOf(values, current);
        int prevIndex = (currentIndex - 1 + values.Length) % values.Length;
        return values[prevIndex];
    }
}
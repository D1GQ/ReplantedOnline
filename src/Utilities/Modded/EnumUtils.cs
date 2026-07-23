using ReplantedOnline.Enums.Versus;

namespace ReplantedOnline.Utilities.Modded;

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
    /// <param name="skip">Optional collection of enum values to skip.</param>
    /// <returns>The next enum value in the sequence, or the first value if current is the last.</returns>
    /// <exception cref="ArgumentException">Thrown if the type parameter is not an enum.</exception>
    internal static T Next<T>(this T current, IEnumerable<T>? skip = null) where T : Enum
    {
        T[] allValues = [.. Enum.GetValues(typeof(T)).Cast<T>()];
        T[] values = skip != null && skip.Any()
            ? [.. allValues.Where(v => !skip.Contains(v))]
            : allValues;

        if (values.Length == 0)
            throw new InvalidOperationException("No values available after applying skip filter.");

        int currentIndex = Array.IndexOf(values, current);
        if (currentIndex == -1 && skip != null && skip.Contains(current))
        {
            return values[0];
        }

        int nextIndex = (currentIndex + 1) % values.Length;
        return values[nextIndex];
    }

    /// <summary>
    /// Gets the previous enum value in the sequence, wrapping around to the last value when at the beginning.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="current">The current enum value.</param>
    /// <param name="skip">Optional collection of enum values to skip.</param>
    /// <returns>The previous enum value in the sequence, or the last value if current is the first.</returns>
    /// <exception cref="ArgumentException">Thrown if the type parameter is not an enum.</exception>
    internal static T Previous<T>(this T current, IEnumerable<T>? skip = null) where T : Enum
    {
        T[] allValues = [.. Enum.GetValues(typeof(T)).Cast<T>()];
        T[] values = skip != null && skip.Any()
            ? [.. allValues.Where(v => !skip.Contains(v))]
            : allValues;

        if (values.Length == 0)
            throw new InvalidOperationException("No values available after applying skip filter.");

        int currentIndex = Array.IndexOf(values, current);
        if (currentIndex == -1 && skip != null && skip.Contains(current))
        {
            return values[^1];
        }

        int prevIndex = (currentIndex - 1 + values.Length) % values.Length;
        return values[prevIndex];
    }

    /// <summary>
    /// Gets the opposite team for a given player team.
    /// </summary>
    /// <param name="team">The player team to get the opposite of.</param>
    /// <returns>
    /// The opposite team:
    /// <list type="bullet">
    /// <item><description>Plants → Zombies</description></item>
    /// <item><description>Zombies → Plants</description></item>
    /// <item><description>Any other value → None</description></item>
    /// </list>
    /// </returns>
    internal static PlayerTeam GetOppositeTeam(this PlayerTeam team)
    {
        switch (team)
        {
            case PlayerTeam.Plants:
                return PlayerTeam.Zombies;
            case PlayerTeam.Zombies:
                return PlayerTeam.Plants;
            default:
                return PlayerTeam.None;
        }
    }

    /// <summary>
    /// Determines if the given arena type is a night arena.
    /// </summary>
    /// <param name="arena">The arena type to check.</param>
    internal static bool IsArenaAtNight(this ArenaType arena)
    {
        return arena == ArenaType.Night || arena == ArenaType.PoolNight || arena == ArenaType.RoofNight;
    }
}
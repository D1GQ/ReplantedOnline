namespace ReplantedOnline.Enums.Versus;

/// <summary>
/// Specifies the different visual and gameplay variations for an arena.
/// </summary>
internal enum ArenaTypes
{
    /// <summary>
    /// Arena is set during daytime.
    /// </summary>
    Day,

    /// <summary>
    /// Arena is set during nighttime.
    /// </summary>
    Night,

    /// <summary>
    /// Arena is set at China!
    /// </summary>
    China,

#if DEBUG
    Debug
#endif
}
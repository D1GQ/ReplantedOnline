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
    /// Arena is set in the backyard pool.
    /// </summary>
    Pool,

    /// <summary>
    /// Arena is set in the backyard pool during the nighttime.
    /// </summary>
    PoolNight,

    /// <summary>
    /// Arena is set on the roof.
    /// </summary>
    Roof,

    /// <summary>
    /// Arena is set on the roof during the nighttime.
    /// </summary>
    RoofNight,

    /// <summary>
    /// Arena is set at China!
    /// </summary>
    China,

#if DEBUG
    Debug
#endif
}
namespace ReplantedOnline.Enums.Versus;

/// <summary>
/// Flags that define custom recommendation behaviors for seed types in versus mode.
/// </summary>
[Flags]
internal enum CustomRecommentedFlags : byte
{
    /// <summary>
    /// No special flags applied.
    /// </summary>
    Recommended = 0,

    /// <summary>
    /// The seed type is not recommended for use in the current arena.
    /// </summary>
    NotRecommended = 1 << 0,

    /// <summary>
    /// The seed type is not allowed to be used in the current arena.
    /// </summary>
    NotAllowed = 1 << 1,

    /// <summary>
    /// The seed type should be excluded from random selection.
    /// </summary>
    ExcludeFromRandom = 1 << 2,

    /// <summary>
    /// The seed type should be excluded from random selection due to dependency requirements.
    /// </summary>
    ExcludeFromRandomDependency = 1 << 3
}
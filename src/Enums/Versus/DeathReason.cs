namespace ReplantedOnline.Enums.Versus;

/// <summary>
/// Specifies the reason for a zombie or plant death in versus mode.
/// </summary>
internal enum DeathReason
{
    /// <summary>
    /// Zombie or plant died through normal gameplay means.
    /// </summary>
    Normal,

    /// <summary>
    /// Zombie or plant was despawned.
    /// </summary>
    Despawn,

    /// <summary>
    /// Zombie or plant died by being burned.
    /// </summary>
    Burned
}
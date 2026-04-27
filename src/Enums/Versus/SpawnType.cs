namespace ReplantedOnline.Enums.Versus;

/// <summary>
/// Defines the spawn behavior types for zombies in versus mode.
/// </summary>
internal enum SpawnType
{
    /// <summary>
    /// No spawn behavior.
    /// </summary>
    None,

    /// <summary>
    /// Zombie rises from the ground.
    /// </summary>
    Rise,

    /// <summary>
    /// Zombie spawns from the back of the arena.
    /// </summary>
    Back,

    /// <summary>
    /// Zombie spawns from the back of the arena with bush shaking effect.
    /// </summary>
    BackAndShakeBushes,

    /// <summary>
    /// Zombie drops from above via bungee zombie.
    /// </summary>
    Bungie
}
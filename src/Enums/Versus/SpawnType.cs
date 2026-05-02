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
    RiseFromGround,

    /// <summary>
    /// Zombie spawns from the back of the arena.
    /// </summary>
    Background,

    /// <summary>
    /// Zombie spawns from the back of the arena with bush shaking effect.
    /// </summary>
    BackgroundAndShakeBushes,

    /// <summary>
    /// Zombie rises from the pool.
    /// </summary>
    RiseFromPool,

    /// <summary>
    /// Zombie drops from above via bungee zombie.
    /// </summary>
    BungeeDropZombie,

    /// <summary>
    /// Zombie drops from above via bungee zombie skiping the target drop animation.
    /// </summary>
    BungeeDropZombieNoTarget
}
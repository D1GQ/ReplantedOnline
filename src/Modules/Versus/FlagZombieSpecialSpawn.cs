using Il2CppReloaded.Gameplay;

namespace ReplantedOnline.Modules.Versus;

/// <summary>
/// Represents a weighted special zombie spawn used for flag zombies.
/// </summary>
internal sealed class FlagZombieSpecialSpawn(ZombieType zombieType, int chance = 100, int decreaseBy = 0)
{
    /// <summary>
    /// The zombie type.
    /// </summary>
    internal readonly ZombieType ZombieType = zombieType;

    /// <summary>
    /// Current chance (0–100) that this spawn will succeed.
    /// This value may decrease over time depending on configuration.
    /// </summary>
    private int _chance = chance;

    /// <summary>
    /// Amount to reduce <see cref="_chance"/> after each successful pick.
    /// </summary>
    private readonly int _decreaseBy = decreaseBy;

    /// <summary>
    /// Attempts to select this zombie type based on its current chance.
    /// If successful, optionally reduces future probability.
    /// </summary>
    /// <returns>
    /// True if this zombie type should be spawned; otherwise false.
    /// </returns>
    internal bool Pick()
    {
        if (_chance <= 0)
            return false;

        if (Common.RandRangeInt(1, 100) <= _chance)
        {
            _chance = Math.Max(_chance - _decreaseBy, 0);
            return true;
        }

        return false;
    }
}

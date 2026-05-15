using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;

namespace ReplantedOnline.Modules.Reloaded.Versus.Configs.Zombie;

[RegisterZombieConfig(ZombieType.DolphinRider)]
internal sealed class DolphinRiderZombieConfig : IZombieConfig
{
    /// <inheritdoc/>
    public void SetArenaDefinition(ZombieDefinition zombieDefinition, ArenaTypes arena) { }

    /// <inheritdoc/>
    public bool IsAllowedInArena(ArenaTypes arena)
    {
        return arena is ArenaTypes.Pool or ArenaTypes.PoolNight;
    }

    /// <inheritdoc/>
    public bool CanBePlacedAt(ArenaTypes arena, int gridX, int gridY)
    {
        // Only in pool
        if (gridY is 2 or 3)
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void OnPlanted(Il2CppReloaded.Gameplay.Zombie zombie, int gridX, int gridY) { }
}

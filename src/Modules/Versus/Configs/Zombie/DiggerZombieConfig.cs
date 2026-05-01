using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;

namespace ReplantedOnline.Modules.Versus.Configs.Zombie;

[RegisterCharacterConfig]
internal sealed class DiggerZombieConfig : IZombieConfig
{
    /// <inheritdoc/>
    public ZombieType Type => ZombieType.Digger;

    /// <inheritdoc/>
    public void SetArenaDefinition(ZombieDefinition zombieDefinition, ArenaTypes arena) { }

    /// <inheritdoc/>
    public bool IsAllowedInArena(ArenaTypes arena)
    {
        if (arena is ArenaTypes.Roof or ArenaTypes.RoofNight)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public bool CanBePlacedAt(ArenaTypes arena, int gridX, int gridY) => true;

    /// <inheritdoc/>
    public void OnPlanted(Il2CppReloaded.Gameplay.Zombie zombie, int gridX, int gridY) { }
}

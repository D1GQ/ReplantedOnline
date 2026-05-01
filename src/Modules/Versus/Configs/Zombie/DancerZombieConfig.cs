using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;

namespace ReplantedOnline.Modules.Versus.Configs.Zombie;

[RegisterCharacterConfig]
internal sealed class DancerZombieConfig : IZombieConfig
{
    /// <inheritdoc/>
    public ZombieType Type => ZombieType.Dancer;

    /// <inheritdoc/>
    public void SetArenaDefinition(ZombieDefinition zombieDefinition, ArenaTypes arena) { }

    /// <inheritdoc/>
    public bool IsAllowedInArena(ArenaTypes arena) => true;

    /// <inheritdoc/>
    public bool CanBePlacedAt(ArenaTypes arena, int gridX, int gridY)
    {
        return gridY != 0 && gridY != 4;
    }

    /// <inheritdoc/>
    public void OnPlanted(Il2CppReloaded.Gameplay.Zombie zombie, int gridX, int gridY) { }
}

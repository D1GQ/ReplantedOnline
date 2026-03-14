using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Interfaces.Versus;

namespace ReplantedOnline.Modules.Versus.Configs.Zombie;

[RegisterCharacterConfig]
internal sealed class TargetZombieConfig : IZombieConfig
{
    /// <inheritdoc/>
    public ZombieType Type => ZombieType.Target;

    /// <inheritdoc/>
    public void SetArenaDefinition(ZombieDefinition zombieDefinition, ArenaTypes arena) { }

    /// <inheritdoc/>
    public bool CanBePlacedAt(ArenaTypes arena, int gridX, int gridY)
    {
        return true;
    }

    /// <inheritdoc/>
    public void OnPlanted(Il2CppReloaded.Gameplay.Zombie zombie, int gridX, int gridY)
    {
        // Fix rendering order
        zombie.RenderOrder -= 200 + 10 * (gridY + 1);
    }
}

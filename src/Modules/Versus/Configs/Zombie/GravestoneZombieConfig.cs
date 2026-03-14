using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Interfaces.Versus;

namespace ReplantedOnline.Modules.Versus.Configs.Zombie;

[RegisterCharacterConfig]
internal sealed class GravestoneZombieConfig : IZombieConfig
{
    /// <inheritdoc/>
    public ZombieType Type => ZombieType.Gravestone;

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
        zombie.RenderOrder -= 100 + 5 * (gridY + 1);
    }
}

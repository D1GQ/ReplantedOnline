using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;

namespace ReplantedOnline.Modules.Reloaded.Versus.Configs.Zombie;

[RegisterZombieConfig(ZombieType.Target)]
internal sealed class TargetZombieConfig : IZombieConfig
{
    /// <inheritdoc/>
    public bool CanBePlacedAt(ArenaType arena, int gridX, int gridY) => true;

    /// <inheritdoc/>
    public void OnPlanted(Il2CppReloaded.Gameplay.Zombie zombie, int gridX, int gridY)
    {
        // Fix rendering order
        zombie.RenderOrder -= 200 + 10 * (gridY + 1);
    }
}

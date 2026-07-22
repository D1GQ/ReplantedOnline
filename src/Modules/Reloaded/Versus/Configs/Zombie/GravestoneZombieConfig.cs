using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;

namespace ReplantedOnline.Modules.Reloaded.Versus.Configs.Zombie;

[RegisterZombieConfig(ZombieType.Gravestone)]
internal sealed class GravestoneZombieConfig : IZombieConfig
{
    /// <inheritdoc/>
    public bool CanBePlacedAt(ArenaTypes arena, int gridX, int gridY) => true;

    /// <inheritdoc/>
    public void OnPlanted(Il2CppReloaded.Gameplay.Zombie zombie, int gridX, int gridY)
    {
        // Fix rendering order
        zombie.RenderOrder -= 100 + 5 * (gridY + 1);
        zombie.mZombieRect = new(50f, 50f, zombie.mZombieRect.width, zombie.mZombieRect.height);
    }
}

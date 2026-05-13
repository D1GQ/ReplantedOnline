using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;

namespace ReplantedOnline.Modules.Reloaded.Versus.Configs.Zombie;

[RegisterZombieConfig(ZombieType.Bungee)]
internal sealed class BungeeZombieConfig : IZombieConfig
{
    /// <inheritdoc/>
    public void SetArenaDefinition(ZombieDefinition zombieDefinition, ArenaTypes arena) { }

    /// <inheritdoc/>
    public bool IsAllowedInArena(ArenaTypes arena) => true;

    /// <inheritdoc/>
    public bool CanBePlacedAt(ArenaTypes arena, int gridX, int gridY) => true;

    /// <inheritdoc/>
    public void OnPlanted(Il2CppReloaded.Gameplay.Zombie zombie, int gridX, int gridY)
    {
        zombie.mRenderOrder -= 400;
    }
}

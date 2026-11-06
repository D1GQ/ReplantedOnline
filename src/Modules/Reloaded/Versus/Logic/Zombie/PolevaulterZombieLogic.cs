using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Structs.Reloaded;

namespace ReplantedOnline.Modules.Reloaded.Versus.Logic.Zombie;

[RegisterZombieLogic(ZombieType.Polevaulter)]
internal sealed class PolevaulterZombieLogic : IZombieLogic
{
    /// <inheritdoc/>
    public bool CanBePlacedAt(ArenaType arena, BoardUnitX boardUnitX, BoardUnitY boardUnitY) => true;

    /// <inheritdoc/>
    public void OnPlanted(Il2CppReloaded.Gameplay.Zombie zombie, BoardUnitX boardUnitX, BoardUnitY boardUnitY)
    {
        // Fix the attack rect for polevaulter zombies on non ground arenas, idk why it's like that, but it is
        zombie.mZombieAttackRect = new(-29f, 0f, 70f, 115f);
    }
}

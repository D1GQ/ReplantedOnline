using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Structs.Reloaded;

namespace ReplantedOnline.Modules.Reloaded.Versus.Logic.Zombie;

[RegisterZombieLogic(ZombieType.Gravestone)]
internal sealed class GravestoneZombieLogic : IZombieLogic
{
    /// <inheritdoc/>
    public bool CanBePlacedAt(ArenaType arena, BoardUnitX boardUnitX, BoardUnitY boardUnitY) => true;

    /// <inheritdoc/>
    public void OnPlanted(Il2CppReloaded.Gameplay.Zombie zombie, BoardUnitX boardUnitX, BoardUnitY boardUnitY)
    {
        // Fix rendering order
        zombie.RenderOrder -= 100 + 5 * (boardUnitY.Grid + 1);
        zombie.mZombieRect = new(50f, 50f, zombie.mZombieRect.width, zombie.mZombieRect.height);
    }
}

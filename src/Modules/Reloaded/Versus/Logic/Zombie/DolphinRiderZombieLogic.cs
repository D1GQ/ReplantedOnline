using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Structs.Reloaded;

namespace ReplantedOnline.Modules.Reloaded.Versus.Logic.Zombie;

[RegisterZombieLogic(ZombieType.DolphinRider)]
internal sealed class DolphinRiderZombieLogic : IZombieLogic
{
    /// <inheritdoc/>
    public bool CanBePlacedAt(ArenaType arena, BoardUnitX boardUnitX, BoardUnitY boardUnitY)
    {
        // Only in pool
        if (boardUnitY.Grid is 2 or 3)
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void OnPlanted(Il2CppReloaded.Gameplay.Zombie zombie, BoardUnitX boardUnitX, BoardUnitY boardUnitY) { }
}

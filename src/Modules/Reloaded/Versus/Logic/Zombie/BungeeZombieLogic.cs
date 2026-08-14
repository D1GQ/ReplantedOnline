using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Structs.Reloaded;

namespace ReplantedOnline.Modules.Reloaded.Versus.Logic.Zombie;

[RegisterZombieLogic(ZombieType.Bungee)]
internal sealed class BungeeZombieLogic : IZombieLogic
{
    /// <inheritdoc/>
    public bool CanBePlacedAt(ArenaType arena, BoardUnitX boardUnitX, BoardUnitY boardUnitY) => true;

    /// <inheritdoc/>
    public void OnPlanted(Il2CppReloaded.Gameplay.Zombie zombie, BoardUnitX boardUnitX, BoardUnitY boardUnitY)
    {
        zombie.mRenderOrder -= 400;
    }
}

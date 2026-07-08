using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Digger)]
internal sealed class DiggerNetworkComponent : ZombieNetworkComponent
{
    internal sealed override void Update()
    {
        if (Net.Zombie?.mZombiePhase is ZombiePhase.DiggerWalking or ZombiePhase.DiggerWalkingWithoutAxe)
        {
            UpdatePositionSync();
        }
    }
}

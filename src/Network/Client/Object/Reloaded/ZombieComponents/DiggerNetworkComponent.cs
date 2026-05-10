using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;

namespace ReplantedOnline.Network.Client.Object.Reloaded.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Digger)]
internal sealed class DiggerNetworkComponent : ZombieNetworkComponent
{
    internal override void Update()
    {
        if (ZombieNetworked._Zombie.mZombiePhase is ZombiePhase.DiggerWalking or ZombiePhase.DiggerWalkingWithoutAxe)
        {
            UpdatePositionSync();
        }
    }
}

using Il2CppReloaded.Gameplay;
using ReplantedOnline.Network.Client.Object.Replanted.Components;

namespace ReplantedOnline.Network.Client.Object.Replanted.ZombieComponents;

/// <inheritdoc/>
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

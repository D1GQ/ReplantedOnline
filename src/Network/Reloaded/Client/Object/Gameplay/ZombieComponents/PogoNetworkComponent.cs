using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Pogo)]
internal sealed class PogoNetworkComponent : ZombieNetworkComponent
{
    internal override void UpdatePosition(float distance, bool useNonNetworkLogic = false)
    {
        if (Net.Zombie == null)
        {
            base.UpdatePosition(distance, useNonNetworkLogic);
            return;
        }

        if (!Net.AmOwner)
        {
            if (Net.Zombie.mZombiePhase is not (ZombiePhase.ZombieNormal or ZombiePhase.PogoHighBounce1))
            {
                base.UpdatePosition(distance, true);
                return;
            }
        }

        base.UpdatePosition(distance, useNonNetworkLogic);
    }
}

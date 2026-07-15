using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.DolphinRider)]
internal sealed class DolphinRiderNetworkComponent : ZombieNetworkComponent
{
    internal sealed override void Update()
    {
        if (Net.Zombie == null) return;

        if (Net.Zombie.mZombiePhase == ZombiePhase.RisingFromGrave) return;

        if (Net.AmOwner)
        {
            if (Net.Zombie.mZombiePhase == ZombiePhase.DolphinInJump && Net.Target == null)
            {
                // Send target to vault
                Plant target = Net.Zombie.FindPlantTarget(ZombieAttackType.Vault);
                Net.SendSetPlantTargetRpc(target);
            }
        }
        else
        {
            if (Net.Zombie.mZombiePhase == ZombiePhase.DolphinInJump)
            {
                SyncedPosX = null;
            }
        }

        // Non owner logic is handled in DolphinRiderZombiePatch.cs

        if (Net.Zombie.mZombiePhase == ZombiePhase.DolphinWalkingWithoutDolphin)
        {
            Net.Target = null;
        }
    }
}

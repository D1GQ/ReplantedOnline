using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;

namespace ReplantedOnline.Network.Client.Object.Reloaded.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Polevaulter)]
internal sealed class PolevaulterNetworkComponent : ZombieNetworkComponent
{
    internal override void Update()
    {
        if (Net.Zombie == null) return;

        if (Net.Zombie.mZombiePhase == ZombiePhase.RisingFromGrave) return;

        if (Net.AmOwner)
        {
            if (Net.Zombie.mZombiePhase == ZombiePhase.PolevaulterInVault && Net.Target == null)
            {
                // Send target to vault
                Plant target = Net.Zombie.FindPlantTarget(ZombieAttackType.Vault);
                Net.SendSetPlantTargetRpc(target);
            }
        }

        // Non owner logic is handled in PolevaulterZombiePatch.cs

        if (Net.Zombie.mZombiePhase == ZombiePhase.PolevaulterPostVault)
        {
            Net.Target = null;
        }

        if (Net.Zombie.mZombiePhase is not ZombiePhase.PolevaulterInVault)
        {
            UpdatePositionSync();
        }
    }
}

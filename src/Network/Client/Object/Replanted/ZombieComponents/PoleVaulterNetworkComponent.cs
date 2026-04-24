using Il2CppReloaded.Gameplay;

namespace ReplantedOnline.Network.Client.Object.Replanted.ZombieComponents;

/// <inheritdoc/>
internal sealed class PoleVaulterNetworkComponent : ZombieNetworkComponent
{
    internal override void Update()
    {
        if (ZombieNetworked._Zombie == null) return;

        if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.RisingFromGrave) return;

        if (ZombieNetworked.AmOwner)
        {
            if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.PolevaulterInVault && ZombieNetworked.Target == null)
            {
                // Send target to vault
                Plant target = ZombieNetworked._Zombie.FindPlantTarget(ZombieAttackType.Vault);
                ZombieNetworked.SendSetPlantTargetRpc(target);
            }
        }

        // Non owner logic is handled in PolevaulterZombiePatch.cs

        if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.PolevaulterPostVault)
        {
            ZombieNetworked.Target = null;
        }

        if (ZombieNetworked._Zombie.mZombiePhase is not ZombiePhase.PolevaulterInVault)
        {
            UpdatePositionSync();
        }
    }
}

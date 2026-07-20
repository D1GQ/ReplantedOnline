using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Polevaulter)]
internal sealed class PolevaulterNetworkComponent : ZombieNetworkComponent
{
    private enum PolevaulterRpcs : byte
    {
        Vault
    }

    private bool _hasVaulted;
    internal sealed override void OnUpdate()
    {
        if (Net.Zombie == null) return;

        if (Net.Zombie.mZombiePhase == ZombiePhase.RisingFromGrave) return;

        if (Net.AmOwner)
        {
            if (Net.Zombie.mZombiePhase == ZombiePhase.PolevaulterInVault && !_hasVaulted)
            {
                _hasVaulted = true;
                SendVaultRpc();
            }
        }
        else
        {
            if (Net.Zombie.mZombiePhase == ZombiePhase.PolevaulterInVault)
            {
                SyncedPosX = null;
            }
        }
    }

    internal override void UpdatePosition(float distance, bool useNonNetworkLogic = false)
    {
        if (Net.Zombie == null)
            return;

        if (Net.Zombie.mZombiePhase == ZombiePhase.PolevaulterInVault)
        {
            return;
        }

        base.UpdatePosition(distance, useNonNetworkLogic);
    }

    private void SendVaultRpc()
    {
        if (Net.Zombie == null)
            return;

        SendNetworkComponentRpc(PolevaulterRpcs.Vault);
    }

    [RpcHandler(PolevaulterRpcs.Vault)]
    private void HandleVaultRpc()
    {
        if (Net.Zombie == null)
            return;

        if (!_hasVaulted)
        {
            _hasVaulted = true;
            Net.Zombie.mZombiePhase = ZombiePhase.PolevaulterInVault;
            Net.Zombie.PlayZombieReanim(Animations.POLEVAULTER_VAULT.Anim, ReanimLoopType.PlayOnceAndHold, Animations.POLEVAULTER_VAULT.Blend, Animations.POLEVAULTER_VAULT.Fps);
        }
    }
}

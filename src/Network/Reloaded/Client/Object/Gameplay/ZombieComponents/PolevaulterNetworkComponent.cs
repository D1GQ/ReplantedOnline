using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Polevaulter)]
internal sealed class PolevaulterNetworkComponent : ZombieNetworkComponent
{
    private bool _hasVaulted;
    internal sealed override void OnUpdate(Zombie zombie)
    {
        if (zombie.mZombiePhase == ZombiePhase.RisingFromGrave)
            return;

        if (Net.AmOwner)
        {
            if (zombie.mZombiePhase == ZombiePhase.PolevaulterInVault && !_hasVaulted)
            {
                _hasVaulted = true;
                RpcVault();
            }
        }
        else
        {
            if (zombie.mZombiePhase == ZombiePhase.PolevaulterInVault)
            {
                SyncedPosX = null;
            }
        }
    }

    protected override void UpdatePosition(Zombie zombie, float distance, bool useNonNetworkLogic = false)
    {
        if (zombie.mZombiePhase == ZombiePhase.PolevaulterInVault)
        {
            return;
        }

        base.UpdatePosition(zombie, distance, useNonNetworkLogic);
    }

    [MethodRpc(LocalExecution.None)]
    private void RpcVault()
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

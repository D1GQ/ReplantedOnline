using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
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
    private bool _hasBeenBlocked;
    internal sealed override void OnUpdate(Zombie zombie)
    {
        if (zombie.mZombiePhase == ZombiePhase.RisingFromGrave)
            return;

        if (Net.AmOwner)
        {
            if (zombie.mZombiePhase == ZombiePhase.PolevaulterInVault)
            {
                if (!_hasVaulted)
                {
                    _hasVaulted = true;
                    RpcVault();
                }
                else
                {
                    SyncedPosX = null;
                }
            }
            else if (zombie.mZombiePhase == ZombiePhase.PolevaulterPostVault)
            {
                if (_hasVaulted && !_hasBeenBlocked)
                {
                    if (zombie.mZombieHeight == ZombieHeight.Falling)
                    {
                        Plant target = zombie.FindPlantTarget(ZombieAttackType.Vault);
                        if (target != null && target.mSeedType == SeedType.Tallnut)
                        {
                            _hasBeenBlocked = true;
                            RpcBlocked(target);
                        }
                    }
                }
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
        var zombie = Net.Zombie;
        if (zombie == null)
            return;

        if (!_hasVaulted)
        {
            _hasVaulted = true;
            zombie.mZombiePhase = ZombiePhase.PolevaulterInVault;
            zombie.PlayZombieReanim(Animations.POLEVAULTER_VAULT.Anim, ReanimLoopType.PlayOnceAndHold, Animations.POLEVAULTER_VAULT.Blend, Animations.POLEVAULTER_VAULT.Fps);
        }
    }

    [MethodRpc(LocalExecution.None)]
    private void RpcBlocked(Plant target)
    {
        if (target == null)
            return;

        var zombie = Net.Zombie;
        if (zombie == null)
            return;

        if (!_hasBeenBlocked && _hasVaulted)
        {
            _hasBeenBlocked = true;
            zombie.mApp.m_audioService.PlayFoley(FoleyType.Bonk);
            zombie.mApp.AddTodParticle(target.mX + 12f, target.mY - 20f, zombie.mRenderOrder + 1, ParticleEffect.TallNutBlock);
            zombie.mZombieHeight = ZombieHeight.Falling;
            zombie.mVelX = 0f;
            zombie.mVelZ = 0f;
            zombie.mPosX = target.mX;
            zombie.mPosY -= 24f;
            zombie.mZombiePhase = ZombiePhase.PolevaulterPostVault;
            zombie.mHasObject = false;
            zombie.StartWalkAnim(0);
        }
    }
}

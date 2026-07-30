using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Catapult)]
internal sealed class CatapultNetworkComponent : ZombieNetworkComponent
{
    private enum CatapultRpcs : byte
    {
        Drive,
        ReadyToFire
    }

    private bool _driving;
    internal bool ReadyToFire;
    private int _lastSummonCounter;
    internal sealed override void OnUpdate(Zombie zombie)
    {
        if (Net.AmOwner)
        {
            if (zombie.mZombiePhase == ZombiePhase.ZombieNormal && !_driving)
            {
                _driving = true;
                Net.Target = null;
                SendDriveRpc();
            }
            else if (zombie.mZombiePhase == ZombiePhase.CatapultLaunching && !ReadyToFire)
            {
                _driving = false;
                ReadyToFire = true;
                Plant plant = zombie.FindCatapultTarget();
                SendReadyToFireRpc(plant, zombie.mSummonCounter);
            }
            else if (zombie.mZombiePhase == ZombiePhase.CatapultReloading)
            {
                ReadyToFire = false;
            }
        }
        else
        {
            if (_driving)
            {
                _driving = false;
                zombie.mZombiePhase = ZombiePhase.ZombieNormal;
                zombie.mPhaseCounter = 0;
                zombie.mTargetPlantID = PlantID.Null;
                zombie.PlayZombieReanim(Animations.CATAPULT_WALK.Anim, ReanimLoopType.Loop, Animations.CATAPULT_WALK.Blend, Animations.CATAPULT_WALK.Fps);
            }
            else if (ReadyToFire)
            {
                ReadyToFire = false;
                zombie.mPhaseCounter = 300;
                zombie.mZombiePhase = ZombiePhase.CatapultLaunching;
                zombie.PlayZombieReanim(Animations.CATAPULT_SHOOT.Anim, ReanimLoopType.PlayOnceAndHold, Animations.CATAPULT_SHOOT.Blend, Animations.CATAPULT_SHOOT.Fps);
            }

            if (zombie.mZombiePhase == ZombiePhase.CatapultLaunching)
            {
                if (zombie.mPhaseCounter <= 165f)
                {
                    zombie.mZombiePhase = ZombiePhase.CatapultReloading;
                    zombie.mPhaseCounter = int.MaxValue;
                    Net.Target = null;
                    zombie.PlayZombieReanim(Animations.CATAPULT_IDLE.Anim, ReanimLoopType.Loop, Animations.CATAPULT_IDLE.Blend, Animations.CATAPULT_IDLE.Fps);
                }
            }


            if (zombie.mZombiePhase == ZombiePhase.ZombieNormal && zombie.mSummonCounter <= 1)
            {
                zombie.mController.SetImageOverride(Animations.CATAPULT_POLE_OBJECT.Slot, Animations.CATAPULT_POLE_OBJECT.Image);
            }
        }
    }

    private void SendDriveRpc()
    {
        SendNetworkComponentRpc(CatapultRpcs.Drive);
    }

    [RpcHandler(CatapultRpcs.Drive)]
    private void HandleDriveRpc()
    {
        _driving = true;
        Net.Target = null;
    }

    private void SendReadyToFireRpc(Plant target, int summonCounter)
    {
        SendNetworkComponentRpc(CatapultRpcs.ReadyToFire, target, summonCounter);
    }

    [RpcHandler(CatapultRpcs.ReadyToFire)]
    private void HandleReadyToFireRpc(Plant target, int summonCounter)
    {
        if (Net.Zombie == null)
            return;

        Net.Target = target;
        _lastSummonCounter = summonCounter;
        Net.Zombie.mSummonCounter = summonCounter;
        ReadyToFire = true;
    }
}
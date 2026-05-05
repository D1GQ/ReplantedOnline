using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Network.Client.Object.Replanted.Components;

namespace ReplantedOnline.Network.Client.Object.Replanted.ZombieComponents;

/// <inheritdoc/>
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
    internal override void Update()
    {
        if (ZombieNetworked._Zombie == null) return;

        if (ZombieNetworked.AmOwner)
        {
            if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.ZombieNormal && !_driving)
            {
                _driving = true;
                ZombieNetworked.Target = null;
                SendDriveRpc();
            }
            else if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.CatapultLaunching && !ReadyToFire)
            {
                _driving = false;
                ReadyToFire = true;
                Plant plant = ZombieNetworked._Zombie.FindCatapultTarget();
                SendReadyToFireRpc(plant, ZombieNetworked._Zombie.mSummonCounter);
            }
            else if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.CatapultReloading)
            {
                ReadyToFire = false;
            }
        }
        else
        {
            if (_driving)
            {
                _driving = false;
                ZombieNetworked._Zombie.mZombiePhase = ZombiePhase.ZombieNormal;
                ZombieNetworked._Zombie.mPhaseCounter = 0;
                ZombieNetworked._Zombie.mTargetPlantID = PlantID.Null;
            }
            else if (ReadyToFire)
            {
                ReadyToFire = false;
                ZombieNetworked._Zombie.mPhaseCounter = 300;
                ZombieNetworked._Zombie.mZombiePhase = ZombiePhase.CatapultLaunching;
                ZombieNetworked._Zombie.PlayZombieReanim("anim_shoot", ReanimLoopType.PlayOnce, 20, 24f);
            }

            if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.CatapultLaunching)
            {
                if (ZombieNetworked._Zombie.mPhaseCounter <= 160f)
                {
                    ZombieNetworked._Zombie.mZombiePhase = ZombiePhase.CatapultReloading;
                    ZombieNetworked._Zombie.mPhaseCounter = int.MaxValue;
                    ZombieNetworked.Target = null;
                }
            }
        }

        UpdatePositionSync();
    }

    private void SendDriveRpc()
    {
        SendNetworkComponentRpc(CatapultRpcs.Drive);
    }

    [RpcHandler(CatapultRpcs.Drive)]
    private void HandleDriveRpc()
    {
        _driving = true;
        ZombieNetworked.Target = null;
    }

    private void SendReadyToFireRpc(Plant target, int summonCounter)
    {
        SendNetworkComponentRpc(CatapultRpcs.ReadyToFire, target, summonCounter);
    }

    [RpcHandler(CatapultRpcs.ReadyToFire)]
    private void HandleReadyToFireRpc(Plant target, int summonCounter)
    {
        ZombieNetworked.Target = target;
        _lastSummonCounter = summonCounter;
        ZombieNetworked._Zombie.mSummonCounter = summonCounter;
        ReadyToFire = true;
    }
}
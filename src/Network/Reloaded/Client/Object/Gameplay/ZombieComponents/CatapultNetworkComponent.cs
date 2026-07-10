using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
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
    internal sealed override void Update()
    {
        if (Net.Zombie == null) return;

        if (Net.AmOwner)
        {
            if (Net.Zombie.mZombiePhase == ZombiePhase.ZombieNormal && !_driving)
            {
                _driving = true;
                Net.Target = null;
                SendDriveRpc();
            }
            else if (Net.Zombie.mZombiePhase == ZombiePhase.CatapultLaunching && !ReadyToFire)
            {
                _driving = false;
                ReadyToFire = true;
                Plant plant = Net.Zombie.FindCatapultTarget();
                SendReadyToFireRpc(plant, Net.Zombie.mSummonCounter);
            }
            else if (Net.Zombie.mZombiePhase == ZombiePhase.CatapultReloading)
            {
                ReadyToFire = false;
            }
        }
        else
        {
            if (_driving)
            {
                _driving = false;
                Net.Zombie.mZombiePhase = ZombiePhase.ZombieNormal;
                Net.Zombie.mPhaseCounter = 0;
                Net.Zombie.mTargetPlantID = PlantID.Null;
            }
            else if (ReadyToFire)
            {
                ReadyToFire = false;
                Net.Zombie.mPhaseCounter = 300;
                Net.Zombie.mZombiePhase = ZombiePhase.CatapultLaunching;
                Net.Zombie.PlayZombieReanim("anim_shoot", ReanimLoopType.PlayOnce, 20, 24f);
            }

            if (Net.Zombie.mZombiePhase == ZombiePhase.CatapultLaunching)
            {
                if (Net.Zombie.mPhaseCounter <= 160f)
                {
                    Net.Zombie.mZombiePhase = ZombiePhase.CatapultReloading;
                    Net.Zombie.mPhaseCounter = int.MaxValue;
                    Net.Target = null;
                }
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
        Net.Target = target;
        _lastSummonCounter = summonCounter;
        Net.Zombie?.mSummonCounter = summonCounter;
        ReadyToFire = true;
    }
}
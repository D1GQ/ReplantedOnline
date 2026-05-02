using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client.Object.Replanted.Components;

namespace ReplantedOnline.Network.Client.Object.Replanted.ZombieComponents;

/// <inheritdoc/>
internal sealed class BungeeNetworkComponent : ZombieNetworkComponent
{
    private enum BungeeRpcs : byte
    {
        Dive,
        TakePlant
    }

    private bool _isDiving;
    private bool _isTakingPlant;
    internal override void Update()
    {
        if (ZombieNetworked._Zombie == null) return;

        SeedPacketDefinitions.SetBungeeRenderOrder(ZombieNetworked._Zombie);

        if (ZombieNetworked.AmOwner)
        {
            if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.BungeeDivingScreaming)
            {
                if (!_isDiving)
                {
                    _isDiving = true;
                    SendDiveRpc();
                }
            }
            else if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.BungeeAtBottom)
            {
                if (ZombieNetworked._Zombie.mPhaseCounter < 10 && !_isTakingPlant)
                {
                    _isTakingPlant = true;
                    SendTakePlantRpc();
                    ZombieNetworked.DespawnAndDestroyWhenNullOrDead(true);
                }
            }
        }
        else
        {
            if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.BungeeDiving)
            {
                if (!_isDiving)
                {
                    ZombieNetworked._Zombie.mPhaseCounter = int.MaxValue;
                }
            }
            else if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.BungeeAtBottom)
            {
                if (!_isTakingPlant)
                {
                    ZombieNetworked._Zombie.mPhaseCounter = int.MaxValue;
                }
            }
            else if (ZombieNetworked._Zombie.mAltitude > 500 && !ZombieNetworked.IsReadyToDespawn)
            {
                ZombieNetworked.IsReadyToDespawn = true;
            }
        }
    }

    private void SendDiveRpc()
    {
        SendNetworkComponentRpc(BungeeRpcs.Dive);
    }

    [RpcHandler(BungeeRpcs.Dive)]
    private void HandleDiveRpc()
    {
        _isDiving = true;
        ZombieNetworked._Zombie.mPhaseCounter = 0;
    }

    private void SendTakePlantRpc()
    {
        SendNetworkComponentRpc(BungeeRpcs.TakePlant);
    }

    [RpcHandler(BungeeRpcs.TakePlant)]
    private void HandleTakePlantRpc()
    {
        _isTakingPlant = true;
        ZombieNetworked._Zombie.mPhaseCounter = 0;
    }
}

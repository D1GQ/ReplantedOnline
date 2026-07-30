using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Bungee)]
internal sealed class BungeeNetworkComponent : ZombieNetworkComponent
{
    private enum BungeeRpcs : byte
    {
        Dive,
        TakePlant
    }

    private bool _isDiving;
    private bool _isTakingPlant;
    internal sealed override void OnUpdate(Zombie zombie)
    {
        SeedPacketDefinitions.SetBungeeRenderOrder(zombie);

        if (Net.AmOwner)
        {
            if (zombie.mZombiePhase == ZombiePhase.BungeeDivingScreaming)
            {
                if (!_isDiving)
                {
                    _isDiving = true;
                    SendDiveRpc();
                }
            }
            else if (zombie.mZombiePhase == ZombiePhase.BungeeAtBottom)
            {
                if (zombie.mPhaseCounter < 10 && !_isTakingPlant)
                {
                    _isTakingPlant = true;
                    SendTakePlantRpc();
                    Net.DespawnAndDestroyWhenNullOrDead(true);
                }
            }
        }
        else
        {
            if (zombie.mZombiePhase == ZombiePhase.BungeeDiving)
            {
                if (!_isDiving)
                {
                    zombie.mPhaseCounter = int.MaxValue;
                }
            }
            else if (zombie.mZombiePhase == ZombiePhase.BungeeAtBottom)
            {
                if (!_isTakingPlant)
                {
                    zombie.mPhaseCounter = int.MaxValue;
                }
            }
            else if (zombie.mAltitude > 500 && !Net.IsReadyToDespawn)
            {
                Net.IsReadyToDespawn = true;
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
        Net.Zombie?.mPhaseCounter = 0;
    }

    private void SendTakePlantRpc()
    {
        SendNetworkComponentRpc(BungeeRpcs.TakePlant);
    }

    [RpcHandler(BungeeRpcs.TakePlant)]
    private void HandleTakePlantRpc()
    {
        _isTakingPlant = true;
        Net.Zombie?.mPhaseCounter = 0;
    }
}

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
    internal sealed override void Update()
    {
        if (Net.Zombie == null) return;

        SeedPacketDefinitions.SetBungeeRenderOrder(Net.Zombie);

        if (Net.AmOwner)
        {
            if (Net.Zombie.mZombiePhase == ZombiePhase.BungeeDivingScreaming)
            {
                if (!_isDiving)
                {
                    _isDiving = true;
                    SendDiveRpc();
                }
            }
            else if (Net.Zombie.mZombiePhase == ZombiePhase.BungeeAtBottom)
            {
                if (Net.Zombie.mPhaseCounter < 10 && !_isTakingPlant)
                {
                    _isTakingPlant = true;
                    SendTakePlantRpc();
                    Net.DespawnAndDestroyWhenNullOrDead(true);
                }
            }
        }
        else
        {
            if (Net.Zombie.mZombiePhase == ZombiePhase.BungeeDiving)
            {
                if (!_isDiving)
                {
                    Net.Zombie.mPhaseCounter = int.MaxValue;
                }
            }
            else if (Net.Zombie.mZombiePhase == ZombiePhase.BungeeAtBottom)
            {
                if (!_isTakingPlant)
                {
                    Net.Zombie.mPhaseCounter = int.MaxValue;
                }
            }
            else if (Net.Zombie.mAltitude > 500 && !Net.IsReadyToDespawn)
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

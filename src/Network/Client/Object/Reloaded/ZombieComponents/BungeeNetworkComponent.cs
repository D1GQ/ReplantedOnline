using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;

namespace ReplantedOnline.Network.Client.Object.Reloaded.ZombieComponents;

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
    internal override void Update()
    {
        if (Net._Zombie == null) return;

        SeedPacketDefinitions.SetBungeeRenderOrder(Net._Zombie);

        if (Net.AmOwner)
        {
            if (Net._Zombie.mZombiePhase == ZombiePhase.BungeeDivingScreaming)
            {
                if (!_isDiving)
                {
                    _isDiving = true;
                    SendDiveRpc();
                }
            }
            else if (Net._Zombie.mZombiePhase == ZombiePhase.BungeeAtBottom)
            {
                if (Net._Zombie.mPhaseCounter < 10 && !_isTakingPlant)
                {
                    _isTakingPlant = true;
                    SendTakePlantRpc();
                    Net.DespawnAndDestroyWhenNullOrDead(true);
                }
            }
        }
        else
        {
            if (Net._Zombie.mZombiePhase == ZombiePhase.BungeeDiving)
            {
                if (!_isDiving)
                {
                    Net._Zombie.mPhaseCounter = int.MaxValue;
                }
            }
            else if (Net._Zombie.mZombiePhase == ZombiePhase.BungeeAtBottom)
            {
                if (!_isTakingPlant)
                {
                    Net._Zombie.mPhaseCounter = int.MaxValue;
                }
            }
            else if (Net._Zombie.mAltitude > 500 && !Net.IsReadyToDespawn)
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
        Net._Zombie.mPhaseCounter = 0;
    }

    private void SendTakePlantRpc()
    {
        SendNetworkComponentRpc(BungeeRpcs.TakePlant);
    }

    [RpcHandler(BungeeRpcs.TakePlant)]
    private void HandleTakePlantRpc()
    {
        _isTakingPlant = true;
        Net._Zombie.mPhaseCounter = 0;
    }
}

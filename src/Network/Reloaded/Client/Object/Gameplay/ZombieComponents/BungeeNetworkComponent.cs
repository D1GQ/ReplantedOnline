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
    private enum BungeeMsgs : byte
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
                    SendDiveMessage();
                }
            }
            else if (zombie.mZombiePhase == ZombiePhase.BungeeAtBottom)
            {
                if (zombie.mPhaseCounter < 10 && !_isTakingPlant)
                {
                    _isTakingPlant = true;
                    SendTakePlantMessage();
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

    private void SendDiveMessage()
    {
        SendMessage(BungeeMsgs.Dive);
    }

    [ObjectMessageHandler(BungeeMsgs.Dive)]
    private void HandleDiveMessage()
    {
        _isDiving = true;
        Net.Zombie?.mPhaseCounter = 0;
    }

    private void SendTakePlantMessage()
    {
        SendMessage(BungeeMsgs.TakePlant);
    }

    [ObjectMessageHandler(BungeeMsgs.TakePlant)]
    private void HandleTakePlantMessage()
    {
        _isTakingPlant = true;
        Net.Zombie?.mPhaseCounter = 0;
    }
}

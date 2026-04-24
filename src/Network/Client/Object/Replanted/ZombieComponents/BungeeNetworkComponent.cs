using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Network.Client.Object.Replanted.Components;

namespace ReplantedOnline.Network.Client.Object.Replanted.ZombieComponents;

/// <inheritdoc/>
internal sealed class BungeeNetworkComponent : ZombieNetworkComponent
{
    private enum BungeeRpcs : byte
    {
        DownOnPlant
    }

    private bool isDownOnPlant;
    internal override void Update()
    {
        if (ZombieNetworked._Zombie == null) return;

        if (ZombieNetworked.AmOwner)
        {
            if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.BungeeGrabbing && ZombieNetworked._Zombie.mPhaseCounter < 10 && !isDownOnPlant)
            {
                isDownOnPlant = true;
                SendDownOnPlantRpc();
            }
        }
        else
        {
            if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.BungeeGrabbing)
            {
                if (!isDownOnPlant)
                {
                    ZombieNetworked._Zombie.mPhaseCounter = int.MaxValue;
                }
            }
        }
    }

    internal void SendDownOnPlantRpc()
    {
        SendNetworkComponentRpc(BungeeRpcs.DownOnPlant);
    }

    [RpcHandler(BungeeRpcs.DownOnPlant)]
    private void HandleDownOnPlantRpc()
    {
        isDownOnPlant = true;
        ZombieNetworked._Zombie.mPhaseCounter = 0;
    }
}

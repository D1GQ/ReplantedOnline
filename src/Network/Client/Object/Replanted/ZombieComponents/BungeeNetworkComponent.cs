using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Client.Object.Replanted.Components;

namespace ReplantedOnline.Network.Client.Object.Replanted.ZombieComponents;

/// <inheritdoc/>
internal sealed class BungeeNetworkComponent : ZombieNetworkComponent
{
    internal override void Update()
    {
        if (ZombieNetworked._Zombie == null) return;

        if (ZombieNetworked.AmOwner)
        {
            if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.BungeeGrabbing && ZombieNetworked._Zombie.mPhaseCounter < 10 && ZombieNetworked.State is not NetStates.UPDATE_STATE)
            {
                ZombieNetworked.State = NetStates.UPDATE_STATE;
                ZombieNetworked.SendSetStateRpc(NetStates.UPDATE_STATE);
            }
        }
        else
        {
            if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.BungeeGrabbing)
            {
                if (ZombieNetworked.State is not NetStates.UPDATE_STATE)
                {
                    ZombieNetworked._Zombie.mPhaseCounter = int.MaxValue;
                }
                else
                {
                    ZombieNetworked._Zombie.mPhaseCounter = 0;
                }
            }
        }
    }
}

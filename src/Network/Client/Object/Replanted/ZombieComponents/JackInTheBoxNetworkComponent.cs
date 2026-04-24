using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Client.Object.Replanted.Components;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Network.Client.Object.Replanted.ZombieComponents;

/// <inheritdoc/>
internal sealed class JackInTheBoxNetworkComponent : ZombieNetworkComponent
{
    internal override void Update()
    {
        if (ZombieNetworked._Zombie == null) return;

        if (ZombieNetworked.AmOwner)
        {
            if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.JackInTheBoxPopping && ZombieNetworked.State is not NetStates.UPDATE_STATE)
            {
                ZombieNetworked.Dead = true;
                ZombieNetworked.State = NetStates.UPDATE_STATE;
                ZombieNetworked.SendSetStateRpc(NetStates.UPDATE_STATE);
                ZombieNetworked.StartCoroutine(CoroutineUtils.WaitForCondition(() => ZombieNetworked._Zombie == null || ZombieNetworked._Zombie.mDead == true, () =>
                {
                    ZombieNetworked.DespawnAndDestroy();
                }));
            }
        }
        else
        {
            if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.JackInTheBoxRunning)
            {
                if (ZombieNetworked.State is not NetStates.UPDATE_STATE)
                {
                    ZombieNetworked._Zombie.mPhaseCounter = int.MaxValue;
                }
                else
                {
                    ZombieNetworked.Dead = true;
                    ZombieNetworked._Zombie.mPhaseCounter = 0;
                }
            }
        }

        UpdatePositionSync();
    }
}

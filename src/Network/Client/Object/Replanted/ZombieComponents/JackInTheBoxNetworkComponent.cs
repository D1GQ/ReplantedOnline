using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Network.Client.Object.Replanted.Components;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Network.Client.Object.Replanted.ZombieComponents;

/// <inheritdoc/>
internal sealed class JackInTheBoxNetworkComponent : ZombieNetworkComponent
{
    private enum JackInTheBoxRpcs : byte
    {
        Explode
    }

    private bool _isExploding;
    internal override void Update()
    {
        if (ZombieNetworked._Zombie == null) return;

        if (ZombieNetworked.AmOwner)
        {
            if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.JackInTheBoxPopping && !_isExploding)
            {
                ZombieNetworked.Dead = true;
                _isExploding = true;
                SendExplodeRpc();
                ZombieNetworked.StartCoroutine(CoroutineUtils.WaitForCondition(() => ZombieNetworked._Zombie == null || ZombieNetworked._Zombie.mDead == true, ZombieNetworked.DespawnAndDestroy));
            }
        }
        else
        {
            if (ZombieNetworked._Zombie.mZombiePhase == ZombiePhase.JackInTheBoxRunning)
            {
                if (!_isExploding)
                {
                    ZombieNetworked._Zombie.mPhaseCounter = int.MaxValue;
                }
            }
        }

        UpdatePositionSync();
    }

    internal void SendExplodeRpc()
    {
        SendNetworkComponentRpc(JackInTheBoxRpcs.Explode);
    }

    [RpcHandler(JackInTheBoxRpcs.Explode)]
    private void HandleExplodeRpc()
    {
        _isExploding = true;
        ZombieNetworked.Dead = true;
        ZombieNetworked._Zombie.mPhaseCounter = 0;
    }
}

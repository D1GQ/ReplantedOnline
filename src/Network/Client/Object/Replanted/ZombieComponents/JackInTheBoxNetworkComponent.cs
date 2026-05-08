using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Client.Object.Replanted.Components;
using ReplantedOnline.Utilities.Mod;

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
                ZombieNetworked.DespawnAndDestroyWhenNullOrDead(true);
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

    internal override void OnDestroyed()
    {
        int jackInTheBoxCount = 0;
        foreach (var zombie in Instances.GameplayActivity.Board.GetZombies())
        {
            if (ZombieNetworked._Zombie == zombie) continue;
            if (zombie.mZombieType != ZombieType.JackInTheBox) continue;

            jackInTheBoxCount++;
        }

        // Stop JackInThebox song if none are left
        if (jackInTheBoxCount == 0)
        {
            Instances.GameplayActivity.m_audioService.StopFoley(FoleyType.JackInThebox);
        }
    }

    private void SendExplodeRpc()
    {
        SendNetworkComponentRpc(JackInTheBoxRpcs.Explode);
    }

    [RpcHandler(JackInTheBoxRpcs.Explode)]
    private void HandleExplodeRpc()
    {
        _isExploding = true;
        ZombieNetworked.Dead = true;
        ZombieNetworked._Zombie.mPhaseCounter = 0;
        ZombieNetworked.IsReadyToDespawn = true;
    }
}

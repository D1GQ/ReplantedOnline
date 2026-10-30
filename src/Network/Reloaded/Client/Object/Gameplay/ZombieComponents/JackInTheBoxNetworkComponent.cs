using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.JackInTheBox)]
internal sealed class JackInTheBoxNetworkComponent : ZombieNetworkComponent
{
    private bool _isExploding;
    internal sealed override void OnUpdate(Zombie zombie)
    {
        if (Net.AmOwner)
        {
            if (zombie.mZombiePhase == ZombiePhase.JackInTheBoxRunning)
            {
                if (zombie.mPosX < 125)
                {
                    zombie.mPhaseCounter = 0;
                    return;
                }
            }

            if (zombie.mZombiePhase == ZombiePhase.JackInTheBoxPopping && !_isExploding)
            {
                RpcExplode();
            }
        }
        else
        {
            if (zombie.mZombiePhase == ZombiePhase.JackInTheBoxRunning)
            {
                if (!_isExploding)
                {
                    zombie.mPhaseCounter = int.MaxValue;
                }
            }
        }
    }

    internal sealed override void OnDestroyed()
    {
        int jackInTheBoxCount = 0;
        foreach (var zombie in Instances.GameplayActivity.Board.GetZombies())
        {
            if (Net.Zombie == zombie) continue;
            if (zombie.mZombieType != ZombieType.JackInTheBox) continue;

            jackInTheBoxCount++;
        }

        // Stop JackInThebox song if none are left
        if (jackInTheBoxCount == 0)
        {
            Instances.GameplayActivity.m_audioService.StopFoley(FoleyType.JackInThebox);
        }
    }

    [MethodRpc(LocalExecution.Before)]
    private void RpcExplode(MethodRpc.RpcInfo rpcInfo = default!)
    {
        if (_isExploding)
        {
            rpcInfo.Local.CancelSend = true;
            return;
        }

        _isExploding = true;
        Net.Dying = true;

        if (rpcInfo.IsLocal)
        {
            Net.DespawnAndDestroyWhenNullOrDead(true);
        }
        else
        {
            Net.Zombie?.mPhaseCounter = 0;
            Net.IsReadyToDespawn = true;
        }
    }
}

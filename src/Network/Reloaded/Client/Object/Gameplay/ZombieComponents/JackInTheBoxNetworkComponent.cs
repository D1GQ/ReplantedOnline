using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.JackInTheBox)]
internal sealed class JackInTheBoxNetworkComponent : ZombieNetworkComponent
{
    private enum JackInTheBoxRpcs : byte
    {
        Explode
    }

    private bool _isExploding;
    internal sealed override void OnUpdate()
    {
        if (Net.Zombie == null) return;

        if (Net.AmOwner)
        {
            if (Net.Zombie.mZombiePhase == ZombiePhase.JackInTheBoxPopping && !_isExploding)
            {
                SendExplodeRpc();
            }
        }
        else
        {
            if (Net.Zombie.mZombiePhase == ZombiePhase.JackInTheBoxRunning)
            {
                if (!_isExploding)
                {
                    Net.Zombie.mPhaseCounter = int.MaxValue;
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

    private void SendExplodeRpc()
    {
        if (!_isExploding)
        {
            _isExploding = true;
            Net.Dying = true;
            SendNetworkComponentRpc(JackInTheBoxRpcs.Explode);
        }

        Net.DespawnAndDestroyWhenNullOrDead(true);
    }

    [RpcHandler(JackInTheBoxRpcs.Explode)]
    private void HandleExplodeRpc()
    {
        if (!_isExploding)
        {
            _isExploding = true;
            Net.Dying = true;
            Net.Zombie?.mPhaseCounter = 0;
        }

        Net.IsReadyToDespawn = true;
    }
}

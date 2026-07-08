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
    internal sealed override void Update()
    {
        if (Net.Zombie == null) return;

        if (Net.AmOwner)
        {
            if (Net.Zombie.mZombiePhase == ZombiePhase.JackInTheBoxPopping && !_isExploding)
            {
                Net.Dead = true;
                _isExploding = true;
                SendExplodeRpc();
                Net.DespawnAndDestroyWhenNullOrDead(true);
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

        UpdatePositionSync();
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
        SendNetworkComponentRpc(JackInTheBoxRpcs.Explode);
    }

    [RpcHandler(JackInTheBoxRpcs.Explode)]
    private void HandleExplodeRpc()
    {
        _isExploding = true;
        Net.Dead = true;
        Net.Zombie?.mPhaseCounter = 0;
        Net.IsReadyToDespawn = true;
    }
}

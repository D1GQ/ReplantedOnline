using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Managers;
using ReplantedOnline.Network.Client.Object.Replanted.Components;
using UnityEngine;

namespace ReplantedOnline.Network.Client.Object.Replanted.ZombieComponents;

/// <inheritdoc/>
internal sealed class TargetNetworkComponent : ZombieNetworkComponent
{
    private Vector3 _lastPos;
    private bool _hasDead;
    internal override void Update()
    {
        if (ZombieNetworked._Zombie?.mController == null) return;
        _lastPos = ZombieNetworked._Zombie.mController.transform.position;
    }

    internal override void OnDeath(DeathReason deathReason)
    {
        if (deathReason != DeathReason.Normal) return;
        if (_hasDead) return;
        _hasDead = true;

        ReplantedLobby.LobbyData.ZombieLife--;

        if (ReplantedLobby.LobbyData.ZombieLife == 0)
        {
            VersusGameplayManager.EndGame(_lastPos, PlayerTeam.Plants);
        }
    }
}

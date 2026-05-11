using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Managers.Reloaded;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;
using UnityEngine;

namespace ReplantedOnline.Network.Client.Object.Reloaded.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Target)]
internal sealed class TargetNetworkComponent : ZombieNetworkComponent
{
    private Vector3 _lastPos;
    private bool _hasDead;
    internal override void Update()
    {
        if (Net._Zombie?.mController == null) return;
        _lastPos = Net._Zombie.mController.transform.position;
    }

    internal override void OnDeath(DeathReason deathReason)
    {
        if (deathReason != DeathReason.Normal) return;
        if (_hasDead) return;
        _hasDead = true;

        ReloadedLobby.LobbyData.ZombieLife--;

        if (ReloadedLobby.LobbyData.ZombieLife == 0)
        {
            VersusGameplayManager.EndGame(_lastPos, PlayerTeam.Plants);
        }
    }
}

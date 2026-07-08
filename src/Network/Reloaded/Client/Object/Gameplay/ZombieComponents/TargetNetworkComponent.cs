using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Managers.Reloaded;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using UnityEngine;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Target)]
internal sealed class TargetNetworkComponent : ZombieNetworkComponent
{
    private Vector3 _lastPos;
    private bool _hasDead;
    internal sealed override void Update()
    {
        if (Net.Zombie?.mController == null) return;
        _lastPos = Net.Zombie.mController.transform.position;
    }

    internal sealed override void OnDeath(DeathReason deathReason)
    {
        if (ReloadedLobby.LobbyData == null) return;
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

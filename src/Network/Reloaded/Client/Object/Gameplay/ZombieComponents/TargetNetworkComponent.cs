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
    internal sealed override void OnUpdate(Zombie zombie)
    {
        if (zombie.mController == null)
            return;

        _lastPos = zombie.mController.transform.position;
    }

    internal sealed override void OnDeath(Zombie? zombie, DeathReason deathReason)
    {
        if (zombie == null)
            return;

        if (ReloadedLobby.LobbyData == null)
            return;

        if (deathReason != DeathReason.Normal)
            return;

        if (_hasDead)
            return;

        _hasDead = true;

        ReloadedLobby.LobbyData.ZombieLife--;

        if (ReloadedLobby.LobbyData.ZombieLife == 0)
        {
            VersusGameplayManager.EndGame(_lastPos, PlayerTeam.Plants);
        }
    }
}

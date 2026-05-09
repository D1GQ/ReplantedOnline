using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;

namespace ReplantedOnline.Network.Client.Object.Reloaded.ZombieComponents;

/// <inheritdoc/>
internal sealed class GravestoneNetworkComponent : ZombieNetworkComponent
{
    internal override void Update()
    {
        // Do not sync position
    }

    internal override void OnDeath(DeathReason deathReason)
    {
        Instances.GameplayActivity.Board.m_vsGravestones.Remove(ZombieNetworked._Zombie);
        ZombieNetworked._Zombie.mGraveX = 0;
        ZombieNetworked._Zombie.mGraveY = 0;
    }
}

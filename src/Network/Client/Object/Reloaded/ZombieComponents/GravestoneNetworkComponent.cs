using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;

namespace ReplantedOnline.Network.Client.Object.Reloaded.ZombieComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(ZombieType.Gravestone)]
internal sealed class GravestoneNetworkComponent : ZombieNetworkComponent
{
    internal override void Update()
    {
        // Do not sync position
    }

    internal override void OnDeath(DeathReason deathReason)
    {
        Instances.GameplayActivity.Board.m_vsGravestones.Remove(Net._Zombie);
        Net._Zombie.mGraveX = 0;
        Net._Zombie.mGraveY = 0;
    }
}

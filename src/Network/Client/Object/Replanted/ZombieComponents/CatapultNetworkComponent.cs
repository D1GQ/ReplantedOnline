namespace ReplantedOnline.Network.Client.Object.Replanted.ZombieComponents;

/// <inheritdoc/>
internal sealed class CatapultNetworkComponent : ZombieNetworkComponent
{
    internal override void Update()
    {
        if (ZombieNetworked._Zombie == null) return;

        if (ZombieNetworked.AmOwner)
        {
            var target = ZombieNetworked._Zombie.FindCatapultTarget();
            if (ZombieNetworked.Target != target)
            {
                ZombieNetworked.SendSetPlantTargetRpc(target);
            }
        }

        UpdatePositionSync();
    }
}
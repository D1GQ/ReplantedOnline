using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

/// <inheritdoc/>
[RegisterNetworkComponent(SeedType.Cherrybomb)]
[RegisterNetworkComponent(SeedType.Jalapeno)]
[RegisterNetworkComponent(SeedType.Doomshroom)]
[RegisterNetworkComponent(SeedType.Iceshroom)]
[RegisterNetworkComponent(SeedType.InstantCoffee)]
internal class PlantSpecialNetworkComponent : PlantNetworkComponent
{
    private bool _isDoingSpecial;
    internal override void OnUpdate(Plant plant)
    {
        if (Net.AmOwner)
        {
            if (!plant.mIsAsleep &&
                plant.mDoSpecialCountdown < 5)
            {
                if (!_isDoingSpecial)
                {
                    RpcDoSpecial();
                }
            }
        }
        else
        {
            plant.mDoSpecialCountdown = int.MaxValue;
        }
    }

    [MethodRpc(LocalExecution.Before)]
    internal void RpcDoSpecial(MethodRpc.RpcInfo rpcInfo = default!)
    {
        if (_isDoingSpecial)
        {
            rpcInfo.Local.CancelSend = true;
            return;
        }

        _isDoingSpecial = true;

        if (rpcInfo.IsLocal)
        {
            Net.Dying = true;
            DoSpecial_PlantSide();
            Net.DespawnAndDestroyWhenDeadOrNull(true);
        }
        else
        {
            DoSpecial();
            Net.IsReadyToDespawn = true;
        }
    }

    protected virtual void DoSpecial()
    {
        if (!Net.Dying)
        {
            Net.Dying = true;
            Net.Plant?.DoSpecial();
        }
    }

    protected virtual void DoSpecial_PlantSide() { }
}
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

/// <inheritdoc/>
[RegisterNetworkComponent(SeedType.Cherrybomb)]
[RegisterNetworkComponent(SeedType.Jalapeno)]
[RegisterNetworkComponent(SeedType.Doomshroom)]
[RegisterNetworkComponent(SeedType.Iceshroom)]
[RegisterNetworkComponent(SeedType.InstantCoffee)]
internal class PlantSpecialNetworkComponent : PlantNetworkComponent
{
    private enum PlantSpecialRpcs : byte
    {
        DoSpecial = 255
    }

    private bool _isDoingSpecial;
    internal override void Update()
    {
        if (Net.Plant == null) return;
        if (Net.AmOwner)
        {
            if (!Net.Plant.mIsAsleep &&
                Net.Plant.mDoSpecialCountdown < 5)
            {
                if (!_isDoingSpecial)
                {
                    Net.Dying = true;
                    SendDoSpecialRpc();
                }
            }
        }
        else
        {
            Net.Plant.mDoSpecialCountdown = int.MaxValue;
        }
    }

    internal void SendDoSpecialRpc()
    {
        if (!_isDoingSpecial)
        {
            _isDoingSpecial = true;
            DoSpecial_PlantSide();
            SendNetworkComponentRpc(PlantSpecialRpcs.DoSpecial);
        }

        Net.DespawnAndDestroyWhenDeadOrNull(true);
    }

    [RpcHandler(PlantSpecialRpcs.DoSpecial)]
    protected void HandleDoSpecialRpc()
    {
        if (!_isDoingSpecial)
        {
            _isDoingSpecial = true;
            DoSpecial();
        }

        Net.IsReadyToDespawn = true;
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
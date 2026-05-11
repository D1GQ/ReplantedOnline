using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;

namespace ReplantedOnline.Network.Client.Object.Reloaded.Components;

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
        if (Net.AmOwner)
        {
            if (!Net._Plant.mIsAsleep &&
                Net._Plant.mDoSpecialCountdown < 5)
            {
                if (!_isDoingSpecial)
                {
                    SendDoSpecialRpc();
                    Net.Dead = true;
                    Net.DespawnAndDestroyWhenDeadOrNull(true);
                }
            }
        }
        else
        {
            Net._Plant.mDoSpecialCountdown = int.MaxValue;
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
    }

    [RpcHandler(PlantSpecialRpcs.DoSpecial)]
    internal void HandleDoSpecialRpc()
    {
        if (!_isDoingSpecial)
        {
            _isDoingSpecial = true;
            DoSpecial();
            Net.IsReadyToDespawn = true;
        }
    }

    protected virtual void DoSpecial()
    {
        Net.Dead = true;
        Net._Plant.DoSpecial();
    }

    protected virtual void DoSpecial_PlantSide() { }
}
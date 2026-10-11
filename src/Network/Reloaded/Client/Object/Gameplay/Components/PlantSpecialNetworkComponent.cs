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
    private enum PlantSpecialMsgs : byte
    {
        DoSpecial
    }

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
                    Net.Dying = true;
                    SendDoSpecialMessage();
                }
            }
        }
        else
        {
            plant.mDoSpecialCountdown = int.MaxValue;
        }
    }

    internal void SendDoSpecialMessage()
    {
        if (!_isDoingSpecial)
        {
            _isDoingSpecial = true;
            DoSpecial_PlantSide();
            SendMessage(PlantSpecialMsgs.DoSpecial);
        }

        Net.DespawnAndDestroyWhenDeadOrNull(true);
    }

    [ObjectMessageHandler(PlantSpecialMsgs.DoSpecial)]
    protected void HandleDoSpecialMessage()
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
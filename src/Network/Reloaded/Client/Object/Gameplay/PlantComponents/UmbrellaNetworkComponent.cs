using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Plants;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.PlantComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(SeedType.Umbrella)]
internal sealed class UmbrellaNetworkComponent : PlantNetworkComponent
{
    private enum UmbrellaMsgs : byte
    {
        HitAndDoSpecial
    }

    internal void SendHitAndDoSpecialMessage()
    {
        SendMessage(UmbrellaMsgs.HitAndDoSpecial);
    }

    [ObjectMessageHandler(UmbrellaMsgs.HitAndDoSpecial)]
    private void HandleHitAndDoSpecialMessage()
    {
        Net.Plant?.DoSpecialOriginal();
    }
}

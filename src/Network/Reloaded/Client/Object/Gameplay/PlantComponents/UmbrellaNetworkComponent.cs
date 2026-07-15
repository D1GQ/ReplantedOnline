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
    private enum UmbrellaRpcs : byte
    {
        HitAndDoSpecial
    }

    internal void SendHitAndDoSpecialRpc()
    {
        SendNetworkComponentRpc(UmbrellaRpcs.HitAndDoSpecial);
    }

    [RpcHandler(UmbrellaRpcs.HitAndDoSpecial)]
    private void HandleHitAndDoSpecialRpc()
    {
        Net.Plant?.DoSpecialOriginal();
    }
}

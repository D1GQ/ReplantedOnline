using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Plants;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.PlantComponents;

/// <inheritdoc/>
[RegisterNetworkComponent(SeedType.Umbrella)]
internal sealed class UmbrellaNetworkComponent : PlantNetworkComponent
{
    [MethodRpc(LocalExecution.None)]
    internal void RpcHitAndDoSpecial()
    {
        Net.Plant?.DoSpecialOriginal();
    }
}

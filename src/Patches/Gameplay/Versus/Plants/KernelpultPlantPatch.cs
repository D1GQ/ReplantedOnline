using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppSource.Controllers;
using ReplantedOnline.Network.Client;

namespace ReplantedOnline.Patches.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class KernelpultPlantPatch
{
    [HarmonyPatch(typeof(ReloadedController), nameof(ReloadedController.AssignRenderGroupToPrefix))]
    [HarmonyPrefix]
    private static bool ReloadedController_AssignRenderGroupToPrefix_Prefix(ReloadedController __instance)
    {
        if (ReplantedLobby.AmInLobby())
        {
            if (__instance is PlantController controller)
            {
                // KernelpultNetworkComponent.cs now handles butter visuals
                if (controller.m_plant != null && controller.m_plant.mSeedType == SeedType.Kernelpult)
                {
                    return false;
                }
            }
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(ReloadedController), nameof(ReloadedController.AssignRenderGroupToPrefix))]
    internal static void AssignRenderGroupToPrefixOriginal(this ReloadedController __instance, string item, int renderGroup)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }
}

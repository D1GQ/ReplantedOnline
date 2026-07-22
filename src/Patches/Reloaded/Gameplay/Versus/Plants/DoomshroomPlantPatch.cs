using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Network.Reloaded.Client;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class DoomshroomPlantPatch
{
    [HarmonyPatch(typeof(GridItem), nameof(GridItem.Update))]
    [HarmonyPostfix]
    private static void GridItem_Update_Postfix(GridItem __instance)
    {
        if (__instance.mGridItemType != GridItemType.Crater)
            return;

        if (ReloadedLobby.AmInLobby())
        {
            __instance.mGridItemCounter = int.MaxValue;
        }
    }
}
using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Network.Client;

namespace ReplantedOnline.Patches.Client;

[HarmonyPatch]
internal static class UserPatch
{
    [HarmonyPatch(typeof(UserService), nameof(UserService.IsCoopModeAvailable))]
    [HarmonyPostfix]
    private static void UserService_IsCoopModeAvailable_Postfix(ref bool __result)
    {
        // Force enable coop mode for online play
        __result = true;
    }

    [HarmonyPatch(typeof(UserService), nameof(UserService.GetPurchases), [typeof(StoreItem)])]
    [HarmonyPostfix]
    private static void UserService_GetPurchases_Postfix(StoreItem item, ref int __result)
    {
        if (ReplantedLobby.AmInLobby())
        {
            // Force enable roof and pool cleaner for online play
            if (item is StoreItem.RoofCleaner or StoreItem.PoolCleaner)
            {
                __result = 1;
            }
        }
    }
}
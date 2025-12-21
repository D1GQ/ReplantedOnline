using HarmonyLib;
using Il2CppReloaded.Services;

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
}
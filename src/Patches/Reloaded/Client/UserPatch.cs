using HarmonyLib;
using Il2CppReloaded.Services;

namespace ReplantedOnline.Patches.Reloaded.Client;

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

    [HarmonyPatch(typeof(UserService), nameof(UserService.IsUnlocked))]
    [HarmonyPrefix]
    private static bool UserService_IsUnlocked_Prefix(ref bool __result)
    {
        // Bypass hardcoded index range check to allow CustomSeedType!
        __result = true;
        return false;
    }
}
using HarmonyLib;
using Il2CppReloaded.Services;

namespace ReplantedOnline.Patches.Client;

[HarmonyPatch]
internal static class UserPatch
{
    [HarmonyPatch(typeof(UserService), nameof(UserService.IsCoopModeAvailable))]
    [HarmonyPostfix]
    internal static void IsCoopModeAvailable_Postfix(ref bool __result)
    {
        // Force enable coop mode for online play
        __result = true;
    }

    [HarmonyPatch(typeof(UserProfile), nameof(UserProfile.mHasSeenMultiplayerUnlocked), MethodType.Getter)]
    [HarmonyPostfix]
    internal static void MULTIPLAYER_UNLOCK_Postfix(ref bool __result)
    {
        // Unlock multiplayer so players can access online features
        __result = true;
    }
}
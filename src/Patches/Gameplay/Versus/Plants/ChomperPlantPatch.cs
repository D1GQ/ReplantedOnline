using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Patches.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class ChomperPlantPatch
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.FindTargetZombie))]
    [HarmonyPrefix]
    private static bool FindTargetZombie_Prefix(Plant __instance)
    {
        if (__instance.mSeedType is not SeedType.Chomper) return true;

        // Check if we're in an online multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            // If player is NOT on the plant side
            if (!VersusState.AmPlantSide)
            {
                // The chomper targeting/animations will be handled by network code
                return false;
            }
        }

        return true;
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.FindTargetZombie))]
    [HarmonyPostfix]
    private static void FindTargetZombie_Postfix(Plant __instance, Zombie __result)
    {
        if (__instance.mSeedType is not SeedType.Chomper) return;

        // Check if we're in an online multiplayer lobby
        if (NetLobby.AmInLobby())
        {
            // Only plant-side players need to send network updates
            if (VersusState.AmPlantSide)
            {
                if (__result != null)
                {
                    if (__result.mZombieType is not (ZombieType.Gargantuar or ZombieType.RedeyeGargantuar))
                    {
                        var netZombie = __result.GetNetworked<ZombieNetworked>();
                        if (netZombie != null && !netZombie.Dead)
                        {
                            netZombie.SendDieNoLootRpc();
                        }
                    }
                }
            }
        }
    }
}
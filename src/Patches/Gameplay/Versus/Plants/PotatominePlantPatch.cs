using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Client.Object.Replanted.PlantComponents;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;
using ReplantedOnline.Utilities;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Patches.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class PotatominePlantPatch
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.FindTargetZombie))]
    [HarmonyPrefix]
    private static bool Plant_FindTargetZombie_Prefix(Plant __instance)
    {
        if (__instance.mSeedType != SeedType.Potatomine) return true;

        // Check if we're in an online multiplayer lobby
        if (ReplantedLobby.AmInLobby())
        {
            // If player is NOT on the plant side
            if (!VersusState.AmPlantSide)
            {
                return false;
            }
        }

        return true;
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.FindTargetZombie))]
    [HarmonyPostfix]
    private static void Plant_FindTargetZombie_Postfix(Plant __instance, ref Zombie __result)
    {
        if (__instance.mSeedType != SeedType.Potatomine) return;

        // Check if we're in an online multiplayer lobby
        if (ReplantedLobby.AmInLobby())
        {
            var netPlant = __instance.GetNetworked();
            if (netPlant == null) return;

            if (VersusState.AmPlantSide)
            {
                // If the plant found a target zombie
                if (__result != null)
                {
                    __result = null;

                    var potatomineComp = netPlant.GetNetworkComponent<PotatomineNetworkComponent>();
                    if (potatomineComp != null && !potatomineComp.HasExploded)
                    {
                        potatomineComp.ExplodeSynced();
                    }
                }
            }
        }
    }

    private static IEnumerator CoWaitAndDie(Plant plant)
    {
        yield return new WaitForSeconds(2f);
        plant.DieOriginal();
    }
}
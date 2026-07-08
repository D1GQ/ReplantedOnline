using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.PlantComponents;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Networked;
using ReplantedOnline.Utilities.Modded;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class PotatominePlantPatch
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.FindTargetZombie))]
    [HarmonyPrefix]
    private static bool Plant_FindTargetZombie_Prefix(Plant __instance)
    {
        if (__instance.mSeedType != SeedType.Potatomine) return true;

        // Check if we're in an online multiplayer lobby
        if (ReloadedLobby.AmInLobby())
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
    private static void Plant_FindTargetZombie_Postfix(Plant __instance, ref Zombie? __result)
    {
        if (__instance.mSeedType != SeedType.Potatomine) return;

        // Check if we're in an online multiplayer lobby
        if (ReloadedLobby.AmInLobby())
        {
            var plantNetworked = __instance.GetNetworked();
            if (plantNetworked == null) return;

            if (VersusState.AmPlantSide)
            {
                // If the plant found a target zombie
                if (__result != null)
                {
                    __result = null;

                    var potatomineComp = plantNetworked.GetNetworkComponent<PotatomineNetworkComponent>();
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
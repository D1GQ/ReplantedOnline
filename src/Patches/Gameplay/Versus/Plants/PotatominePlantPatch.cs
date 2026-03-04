using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Object.Game;
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
        if (NetLobby.AmInLobby())
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
        if (NetLobby.AmInLobby())
        {
            var netPlant = __instance.GetNetworked<PlantNetworked>();

            if (netPlant != null)
            {
                // PLANT-SIDE PLAYER LOGIC
                if (VersusState.AmPlantSide)
                {
                    // If the plant found a target zombie (original logic worked)
                    if (__result != null)
                    {
                        // Send network message to tell other players about the potato mine target
                        netPlant.SendSetZombieTargetRpc(__result);
                    }
                }
                else
                {
                    // For other players, get the target from network state instead of local AI
                    if (netPlant._Target != null)
                    {
                        // Override the result with the networked zombie target
                        __result = netPlant._Target;
                        netPlant._Target = null;
                        __instance.mController.StartCoroutine(CoWaitAndDie(__instance).WrapToIl2cpp());
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
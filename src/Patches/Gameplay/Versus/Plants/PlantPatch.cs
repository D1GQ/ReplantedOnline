using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Patches.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class PlantPatch
{
    // Stop game from placing initial sunflower in vs
    [HarmonyPatch(typeof(Board), nameof(Board.AddPlant))]
    [HarmonyPrefix]
    private static bool Board_AddPlant_Prefix()
    {
        if (NetLobby.AmInLobby() && Instances.GameplayActivity.VersusMode.m_versusTime < 1f)
        {
            return false;
        }

        return true;
    }
}
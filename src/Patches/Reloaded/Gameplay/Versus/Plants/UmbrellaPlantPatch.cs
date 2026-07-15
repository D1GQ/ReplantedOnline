using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.PlantComponents;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class UmbrellaPlantPatch
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.DoSpecial))]
    [HarmonyPrefix]
    private static bool Plant_DoSpecial_Prefix(Plant __instance)
    {
        if (__instance.mSeedType != SeedType.Umbrella) return true;

        if (ReloadedLobby.AmInLobby())
        {
            if (VersusState.AmPlantSide)
            {
                var plantNetworked = __instance.GetNetworked();
                if (plantNetworked != null)
                {
                    if (plantNetworked.TryGetNetworkComponent<UmbrellaNetworkComponent>(out var comp))
                    {
                        comp.SendHitAndDoSpecialRpc();
                    }
                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    [HarmonyPatch(typeof(Board), nameof(Board.FindUmbrellaPlant))]
    [HarmonyPostfix]
    private static void Board_FindUmbrellaPlant_Postfix(ref Plant? __result)
    {
        if (ReloadedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide)
            {
                if (__result != null && __result.mState == PlantState.Notready)
                {
                    __result = null;
                }
            }
        }
    }
}

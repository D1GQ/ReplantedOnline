using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Modded;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Networked;

[HarmonyPatch]
internal static class PlantSyncPatch
{
    private readonly static ExecuteInterval findTargetAndFireInterval = new();
    [HarmonyPatch(typeof(Plant), nameof(Plant.FindTargetAndFire))]
    [HarmonyPrefix]
    private static bool Plant_FindTargetAndFire_Prefix(Plant __instance, int theRow, ref PlantWeapon thePlantWeapon)
    {
        // Check if we're in an online multiplayer lobby
        if (ReloadedLobby.AmInLobby())
        {
            if (VersusState.AmPlantSide)
            {
                if (findTargetAndFireInterval.Execute())
                {
                    var plantNetworked = __instance.GetNetworked();
                    if (plantNetworked != null)
                    {
                        plantNetworked.SendReadyToFireRpc(theRow, ref thePlantWeapon);
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

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Plant), nameof(Plant.FindTargetAndFire))]
    internal static bool FindTargetAndFireOriginal(this Plant __instance, int theRow, PlantWeapon thePlantWeapon)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    private readonly static ExecuteInterval fireInterval = new();
    [HarmonyPatch(typeof(Plant), nameof(Plant.Fire))]
    [HarmonyPrefix]
    private static bool Plant_Fire_Prefix(Plant __instance, Zombie theTargetZombie, int theRow, ref PlantWeapon thePlantWeapon)
    {
        // Check if we're in an online multiplayer lobby
        if (ReloadedLobby.AmInLobby())
        {
            if (VersusState.AmPlantSide)
            {
                if (fireInterval.Execute())
                {
                    var plantNetworked = __instance.GetNetworked();
                    if (plantNetworked != null)
                    {
                        plantNetworked.SendFireRpc(theTargetZombie, theRow, ref thePlantWeapon);
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

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Plant), nameof(Plant.Fire))]
    internal static void FireOriginal(this Plant __instance, Zombie theTargetZombie, int theRow, PlantWeapon thePlantWeapon)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.Die))]
    [HarmonyPrefix]
    private static bool Plant_Die_Prefix(Plant __instance)
    {
        // Only handle network synchronization if we're in a multiplayer lobby
        if (ReloadedLobby.AmInLobby())
        {
            var plantNetworked = __instance.GetNetworked();
            if (plantNetworked != null)
            {
                if (VersusState.AmPlantSide)
                {
                    plantNetworked.SendDieRpc();
                }
                else
                {
                    if (!plantNetworked.Dying)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Plant), nameof(Plant.Die))]
    internal static void DieOriginal(this Plant __instance)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.Squish))]
    [HarmonyPrefix]
    private static bool Plant_Squish_Prefix(Plant __instance)
    {
        // Only handle network synchronization if we're in a multiplayer lobby
        if (ReloadedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide) return false;

            var plantNetworked = __instance.GetNetworked();
            if (plantNetworked != null)
            {
                plantNetworked.SendSquashPlantRpc();
            }
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Plant), nameof(Plant.Squish))]
    internal static void SquishOriginal(this Plant __instance)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }
}
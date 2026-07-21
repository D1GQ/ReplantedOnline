using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;
using ReplantedOnline.Utilities.Modded;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class YetiZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.FindPlantTarget))]
    [HarmonyPostfix]
    private static void Zombie_FindPlantTarget_Postfix(Zombie __instance, ZombieAttackType theAttackType, ref Plant? __result)
    {
        if (__instance.mZombieType != ZombieType.Yeti) return;
        if (theAttackType != ZombieAttackType.Chew) return;

        if (ReloadedLobby.AmInLobby())
        {
            var zombieNetworked = __instance.GetNetworked();
            if (zombieNetworked != null && zombieNetworked.TryGetNetworkComponent<YetiNetworkComponent>(out var comp))
            {
                if (comp.CurrentState != YetiNetworkComponent.YetiState.Enraged)
                {
                    __result = null;
                }
            }
            else
            {
                __result = null;
            }
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.IsWalkingBackwards))]
    [HarmonyPostfix]
    private static void Zombie_FindPlantTarget_Postfix(Zombie __instance, ref bool __result)
    {
        if (__instance.mZombieType != ZombieType.Yeti) return;

        if (ReloadedLobby.AmInLobby())
        {
            var zombieNetworked = __instance.GetNetworked();
            if (zombieNetworked != null && zombieNetworked.TryGetNetworkComponent<YetiNetworkComponent>(out var comp))
            {
                if (comp.CurrentState == YetiNetworkComponent.YetiState.Runningback)
                {
                    __result = true;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.UpdateAbilities))]
    [HarmonyPatch(typeof(Plant), nameof(Plant.UpdateChomper))] // For some reason UpdateChomper is never used, and all of its logic is copied into Plant.UpdateAbilities()... like why
    [HarmonyPrefix]
    private static bool Plant_UpdateAbilities_Prefix(Plant __instance)
    {
        if (__instance.mSeedType != SeedType.Chomper)
            return true;

        if (ReloadedLobby.AmInLobby())
        {
            // Make Chomper do overtime damage to yeti.
            if (VersusState.AmPlantSide)
            {
                if (__instance.mState != PlantState.ChomperBiting)
                    return true;

                if (__instance.mStateCountdown > 1)
                    return true;

                Zombie target = __instance.FindTargetZombie(__instance.mRow, PlantWeapon.Primary);

                if (target != null && target.mZombieType == ZombieType.Yeti)
                {
                    __instance.mApp.m_audioService.PlayFoley(Il2CppReloaded.Services.FoleyType.BigChomp);
                    __instance.mApp.m_audioService.PlayFoley(Il2CppReloaded.Services.FoleyType.Splat);
                    target.TakeDamage(40, 0);
                    __instance.mState = PlantState.ChomperBitingMissed;
                    return false;
                }
            }
        }

        return true;
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.DragUnder))]
    [HarmonyPriority(Priority.First)]
    [HarmonyPrefix]
    private static bool Zombie_DragUnder_Prefix(Zombie __instance)
    {
        if (__instance.mZombieType != ZombieType.Yeti) return true;

        if (ReloadedLobby.AmInLobby())
        {
            // Make Tanglekelp act like every other insta on Yeti
            if (__instance.mBodyHealth > 1800)
            {
                if (VersusState.AmPlantSide)
                {
                    __instance.TakeDamage(1800, DamageFlags.HitsShieldAndBody);
                }

                foreach (var plant in __instance.mBoard.GetPlants())
                {
                    if (plant.mSeedType != SeedType.Tanglekelp) continue;

                    if (plant.mTargetZombieID == __instance.DataID)
                    {
                        plant.mTargetZombieID = ZombieID.Null;
                    }
                }

                return false;
            }
        }

        return true;
    }
}
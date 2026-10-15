using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Utilities.Modded;
using UnityEngine;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class FumeshroomPlantPatch
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.DoRowAreaDamage))]
    [HarmonyPrefix]
    private static bool Plant_DoRowAreaDamage_Prefix(Plant __instance, int theDamage, DamageFlags theDamageFlags)
    {
        if (__instance.mSeedType != SeedType.Fumeshroom)
            return true;

        if (ReloadedLobby.AmInLobby())
        {
            // From Versus Mode Console:
            // Make Target Zombies and Gravestones invulnerable when behind another gravestone
            // This is a direct fix to Fumeshroom OP piercing logic

            var damageRangeFlags = __instance.GetDamageRangeFlags(PlantWeapon.Primary);
            Rect plantAttackRect = __instance.GetPlantAttackRect(PlantWeapon.Primary);

            foreach (var zombie in __instance.mBoard.GetZombies())
            {
                if (zombie.mRow != __instance.mRow)
                    continue;

                if (zombie.IsOnHighGround() != __instance.IsOnHighGround())
                    continue;

                if (!zombie.EffectedByDamage(damageRangeFlags))
                    continue;

                if (zombie.mZombieType.IsGravestoneOrTarget() && CheckForGravestoneOrTargetAHead(zombie))
                {
                    continue;
                }

                Rect zombieRect = zombie.GetZombieRect();
                if (Common.GetRectOverlap(ref plantAttackRect, ref zombieRect) > 0)
                {
                    zombie.TakeDamage(theDamage, theDamageFlags);
                    zombie.mApp.m_audioService.PlayFoley(FoleyType.Splat);
                }
            }

            return false;
        }

        return true;
    }

    private static bool CheckForGravestoneOrTargetAHead(Zombie target)
    {
        foreach (var zombie in target.mBoard.GetZombies())
        {
            if (!zombie.mZombieType.IsGravestoneOrTarget())
                continue;

            if (zombie.mRow != target.mRow)
                continue;

            if (zombie.mPosX < target.mPosX)
            {
                return true;
            }
        }

        return false;
    }
}
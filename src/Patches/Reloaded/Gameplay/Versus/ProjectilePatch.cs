using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Modded;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Utilities.Modded;
using ReplantedOnline.Utilities.Unity;
using UnityEngine;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus;

[HarmonyPatch]
internal static class ProjectilePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.GetZombieRect))]
    [HarmonyPostfix]
    private static void Zombie_GetZombieRect_Postfix(Zombie __instance, ref Rect __result)
    {
        if (!__instance.mZombieType.IsGravestoneOrTarget())
            return;

        // Check if we're in an online multiplayer lobby
        if (ReloadedLobby.AmInLobby())
        {
            // From Versus Mode Console:
            // Make Target Zombies and Gravestones invulnerable when behind another gravestone
            // This is a direct fix to Fumeshroom OP piercing logic
            foreach (var gravestone in __instance.mBoard.m_vsGravestones)
            {
                if (gravestone == __instance) continue;
                if (gravestone.mRow != __instance.mRow) continue;

                // Check if Gravestone is in front of zombie
                if (gravestone.mPosX < __instance.mPosX)
                {
                    if (gravestone.mZombiePhase != ZombiePhase.RisingFromGrave)
                    {
                        __result = RectUtils.NonInteractableRect;
                        break;
                    }
                }
            }
        }
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.GetZombieRect))]
    private static Rect GetZombieRectOriginal(this Zombie __instance)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.IsZombieHitBySplash))]
    [HarmonyPostfix]
    private static void Projectile_IsZombieHitBySplash_Melonpult_Postfix(Projectile __instance, Zombie theZombie, ref bool __result)
    {
        if (__instance.mProjectileType != ProjectileType.Melon)
            return;

        if (ReloadedLobby.AmInLobby())
        {
            // From Versus Mode Console:
            // Prevent target zombies and gravestones from taking Melonpult splash damage
            if (theZombie.mZombieType.IsGravestoneOrTarget())
            {
                if (__instance.mRow != theZombie.mRow)
                {
                    __result = false;
                    return;
                }

                // Melonpult should be targeting zombies over gravestone
                foreach (var zombie in __instance.mBoard.GetZombies())
                {
                    if (zombie.mZombieType.IsGravestoneOrTarget()) continue;
                    if (zombie.IsDeadOrDying()) continue;
                    var zombieRect = zombie.GetZombieRectOriginal();
                    zombieRect.m_Width += 25;
                    if (!theZombie.GetZombieRectOriginal().Overlaps(zombieRect)) continue;

                    if (zombie.mRow == theZombie.mRow)
                    {
                        __result = false;
                        return;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.FindTargetZombie))]
    [HarmonyPrefix]
    private static bool Plant_FindTargetZombie_Catapult_Prefix(Plant __instance, ref Zombie? __result)
    {
        if (__instance.mSeedType is not (SeedType.Cabbagepult or SeedType.Kernelpult or SeedType.Melonpult))
            return true;

        if (ReloadedLobby.AmInLobby())
        {
            // From Versus Mode Console:
            // Target zombies over gravestones for catapult plants
            PriorityVar<Zombie> zombiePriority = new();
            foreach (var zombie in __instance.mBoard.GetZombies())
            {
                if (zombie.mRow != __instance.mRow) continue;
                if (zombie.IsDeadOrDying()) continue;

                bool targetLast = zombie.mZombieType.IsGravestoneOrTarget();
                if (!targetLast)
                {
                    zombiePriority.Compare(zombie, -zombie.mPosX);
                }
                else
                {
                    zombiePriority.Compare(zombie, -zombie.mPosX - 10000);
                }
            }

            if (zombiePriority.TryGet(out var pZombie))
            {
                __result = pZombie;
                return false;
            }
        }

        return true;
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.FindCollisionTarget))]
    [HarmonyPostfix]
    private static void Projectile_FindCollisionTarge_Catapult_Postfix(Projectile __instance, ref Zombie? __result)
    {
        if (__instance.mProjectileType is not (ProjectileType.Cabbage or ProjectileType.Kernel or
            ProjectileType.Butter or ProjectileType.Wintermelon)) return;

        if (ReloadedLobby.AmInLobby())
        {
            // From Versus Mode Console:
            // Ignore gravestone collision for catapult projectile if zombie is in range
            if (__result != null && __result.mZombieType.IsGravestoneOrTarget())
            {
                PriorityVar<Zombie> zombiePriority = new();
                foreach (var zombie in __instance.mBoard.GetZombies())
                {
                    if (zombie.mZombieType.IsGravestoneOrTarget()) continue;
                    if (zombie.mRow != __instance.mRow) continue;
                    var zombieRect = zombie.GetZombieRectOriginal();
                    zombieRect.m_Width += 25;
                    if (!__result.GetZombieRectOriginal().Overlaps(zombieRect)) continue;

                    zombiePriority.Compare(zombie, -zombie.mPosX);
                }

                if (zombiePriority.TryGet(out var pZombie))
                {
                    __result = pZombie;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.FindTargetZombie))]
    [HarmonyPostfix]
    private static void Plant_FindTargetZombie_Puffshroom_Postfix(Plant __instance, ref Zombie? __result)
    {
        if (__instance.mSeedType != SeedType.Puffshroom)
            return;

        if (ReloadedLobby.AmInLobby())
        {
            // Subject to change depending on balancing
            // Ignore target zombies for puffshrooms
            if (__result != null && __result.mZombieType == ZombieType.Target)
            {
                __result = null;
            }
        }
    }
}

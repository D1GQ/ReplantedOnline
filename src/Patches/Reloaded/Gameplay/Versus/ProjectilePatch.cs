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
                if (zombie.mRow != __instance.mRow)
                    continue;

                if (zombie.IsDeadOrDying() || !zombie.HasNetworked())
                    continue;

                bool targetLast = zombie.mZombieType.IsGravestoneOrTarget();

                if (!targetLast)
                {
                    // For normal zombies: prioritize based on position
                    zombiePriority.Compare(zombie, -zombie.mPosX);
                }
                else
                {
                    // For gravestones/targets: give them significantly lower priority
                    zombiePriority.Compare(zombie, -zombie.mPosX - 10000);
                }
            }

            // After evaluating all zombies, get the highest priority target
            if (zombiePriority.TryGet(out var pZombie))
            {
                __result = pZombie;
                return false;
            }
        }

        return true;
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.IsZombieHitBySplash))]
    [HarmonyPostfix]
    private static void Projectile_IsZombieHitBySplash_Melonpult_Postfix(Projectile __instance, Zombie theZombie, ref bool __result)
    {
        if (__instance.mProjectileType is not (ProjectileType.Melon or ProjectileType.Wintermelon))
            return;

        if (ReloadedLobby.AmInLobby())
        {
            // From Versus Mode Console:
            // Prevent target zombies and gravestones from taking Melonpult splash damage
            if (theZombie.mZombieType.IsGravestoneOrTarget())
            {
                // If the projectile is on a different row than the target, it shouldn't hit
                if (__instance.mRow != theZombie.mRow)
                {
                    __result = false;
                    return;
                }

                foreach (var zombie in __instance.mBoard.GetZombies())
                {
                    if (zombie == theZombie)
                        continue;

                    if (zombie.mRow != theZombie.mRow)
                        continue;

                    if (zombie.IsDeadOrDying() || !zombie.HasNetworked())
                        continue;

                    if (!zombie.mZombieType.IsGravestoneOrTarget())
                    {
                        // If a normal zombie is in front of the target,
                        // the splash should hit the normal zombie instead
                        if (zombie.mPosX < theZombie.mPosX)
                        {
                            __result = false;
                            return;
                        }

                        // Check for collision/overlap between the projectile's splash area and the zombie
                        var rect = __instance.GetProjectileRect();
                        var zombieRect = zombie.GetZombieRectOriginal();
                        zombieRect.m_Width += 25; // Slightly increase hitbox for more lenient detection

                        // If there's overlap the splash hits the zombie
                        // and shouldn't also hit the target/gravestone behind it
                        if (Common.GetRectOverlap(ref rect, ref zombieRect) > 1)
                        {
                            __result = false;
                            return;
                        }
                    }
                    else if (zombie.mPosX < theZombie.mPosX)
                    {
                        // Another target/gravestone in front blocks the splash from reaching this one
                        __result = false;
                        return;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.FindCollisionTarget))]
    [HarmonyPostfix]
    private static void Projectile_FindCollisionTarget_Catapult_Postfix(Projectile __instance, ref Zombie? __result)
    {
        if (__instance.mProjectileType is not (ProjectileType.Cabbage or ProjectileType.Kernel or ProjectileType.Butter
            or ProjectileType.Melon or ProjectileType.Wintermelon)) return;

        if (ReloadedLobby.AmInLobby())
        {
            // From Versus Mode Console:
            // Ignore gravestone collision for catapult projectile if zombie is in range
            if (__result != null && __result.mZombieType.IsGravestoneOrTarget())
            {
                PriorityVar<Zombie> zombiePriority = new();
                foreach (var zombie in __instance.mBoard.GetZombies())
                {
                    if (zombie == __result)
                        continue;

                    if (zombie.mZombieType.IsGravestoneOrTarget())
                        continue;

                    if (zombie.IsDeadOrDying() || !zombie.HasNetworked())
                        continue;

                    if (zombie.mRow != __instance.mRow)
                        continue;

                    // Check if the normal zombie overlaps with or is in front of the gravestone
                    var resultZombieRect = __result.GetZombieRectOriginal();
                    var zombieRect = zombie.GetZombieRectOriginal();
                    zombieRect.m_Width += 25;

                    // Skip zombies that don't overlap with the gravestone and or behind it
                    if (Common.GetRectOverlap(ref resultZombieRect, ref zombieRect) <= 0 &&
                        zombie.mPosX > __result.mPosX)
                        continue;

                    // Add this zombie to the priority queue, prioritizes zombies further left
                    zombiePriority.Compare(zombie, -zombie.mPosX);
                }

                // If we found a valid normal zombie to target instead of the gravestone
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

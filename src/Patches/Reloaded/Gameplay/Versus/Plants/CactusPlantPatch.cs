using HarmonyLib;
using Il2CppReloaded;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.MonoScripts.Unity;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Utilities.Modded;
using UnityEngine;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Plants;

[HarmonyPatch]
internal static class CactusPlantPatch
{
    private static readonly Dictionary<Projectile, SpikeProjectileInfo> ProjectileInfoInstances = [];

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.ProjectileInitialize))]
    [HarmonyPostfix]
    private static void Projectile_ProjectileInitialize_Postfix(Projectile __instance)
    {
        if (__instance.mProjectileType == ProjectileType.Spike)
        {
            var observableGameObject = __instance.mController.gameObject.GetOrAddComponent<ObservableGameObject>();
            observableGameObject.OnGameObjectDestroy = null;
            observableGameObject.OnGameObjectDestroy += go =>
            {
                ProjectileInfoInstances.Remove(__instance);
            };
            ProjectileInfoInstances.Add(__instance, new());
        }
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Die))]
    [HarmonyPrefix]
    private static void Projectile_Die_Prefix(Projectile __instance)
    {
        if (__instance.mProjectileType == ProjectileType.Spike)
        {
            ProjectileInfoInstances.Remove(__instance);
            var observableGameObject = __instance.mController.gameObject.GetOrAddComponent<ObservableGameObject>();
            observableGameObject.OnGameObjectDestroy = null;
        }
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.FindCollisionTarget))]
    [HarmonyPrefix]
    private static bool Projectile_FindCollisionTarget_Prefix(Projectile __instance, ref Zombie __result)
    {
        if (ReloadedLobby.AmInLobby())
        {
            if (__instance.mProjectileType != ProjectileType.Spike)
                return true;

            if (ProjectileInfoInstances.TryGetValue(__instance, out var spikeProjectileInfo))
            {
                var projectileRect = __instance.GetProjectileRect();
                foreach (var zombie in __instance.mBoard.GetZombies())
                {
                    if (__instance.mRow != zombie.mRow)
                        continue;

                    if (!zombie.EffectedByDamage(__instance.mDamageRangeFlags))
                        continue;

                    if (spikeProjectileInfo.ImpactedZombies.Contains(zombie.DataID))
                        continue;

                    var zombieRect = zombie.GetZombieRect();
                    if (Common.GetRectOverlap(ref projectileRect, ref zombieRect) > 0)
                    {
                        __result = zombie;
                        break;
                    }
                }
            }

            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.DoImpact))]
    [HarmonyPrefix]
    private static bool Projectile_DoImpact_Prefix(Projectile __instance, Zombie theZombie)
    {
        if (ReloadedLobby.AmInLobby())
        {
            if (__instance.mProjectileType != ProjectileType.Spike)
                return true;

            if (ProjectileInfoInstances.TryGetValue(__instance, out var spikeProjectileInfo) &&
                !spikeProjectileInfo.ImpactedZombies.Contains(theZombie.DataID))
            {
                int baseDamage = 18;
                int impactedCount = spikeProjectileInfo.ImpactedZombies.Count;
                float damageMultiplier = Mathf.Max(0f, 1f - (0.2f * impactedCount));
                int damage = Mathf.RoundToInt(baseDamage * damageMultiplier);

                if (theZombie.mZombieType.IsGravestoneOrTarget() ||
                    theZombie.mShieldHealth > 0 ||
                    theZombie.mFlyingHealth > 0)
                {
                    theZombie.TakeDamage(damage + (2 * (impactedCount + theZombie.mFlyingHealth)), 0);
                    __instance.PlayImpactSound(theZombie);
                    __instance.Die();

                    return false;
                }

                theZombie.TakeDamage(damage, DamageFlags.BypassesShield);
                __instance.PlayImpactSound(theZombie);

                spikeProjectileInfo.ImpactedZombies.Add(theZombie.DataID);
                if (spikeProjectileInfo.ImpactedZombies.Count >= 3)
                {
                    __instance.Die();
                }
            }

            return false;
        }

        return true;
    }

    private sealed class SpikeProjectileInfo
    {
        internal readonly HashSet<ZombieID> ImpactedZombies = [];
    }
}

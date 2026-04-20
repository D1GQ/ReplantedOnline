using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Patches.Gameplay.Versus;

[HarmonyPatch]
internal static class ProjectilePatch
{
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.IsZombieHitBySplash))]
    [HarmonyPostfix]
    private static void Projectile_IsZombieHitBySplash_Postfix(Projectile __instance, Zombie theZombie, ref bool __result)
    {
        if (__instance.mProjectileType != ProjectileType.Melon) return;

        if (ReplantedLobby.AmInLobby())
        {
            // Prevent target zombies and gravestones from taking Melonpult splash damage
            // Don't have to worry about target zombies and gravestones taking damage from behind due to ZombiePatch.Zombie_GetZombieRect_Postfix
            if (theZombie.mZombieType.IsGravestoneOrTarget())
            {
                if (__instance.mRow != theZombie.mRow)
                {
                    __result = false;
                }
            }
        }
    }
}

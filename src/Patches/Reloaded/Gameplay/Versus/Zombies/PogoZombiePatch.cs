using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;
using ReplantedOnline.Utilities.Modded;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class PogoZombiePatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.FindPlantTarget))]
    [HarmonyPostfix]
    private static void Zombie_FindPlantTarget_Postfix(Zombie __instance, ZombieAttackType theAttackType, ref Plant? __result)
    {
        if (__instance.mZombieType != ZombieType.Pogo) return;
        if (theAttackType != ZombieAttackType.Vault) return;

        if (ReloadedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide)
            {
                __result = null;
            }
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.PogoBreak))]
    [HarmonyPrefix]
    private static bool Zombie_PogoBreak_Prefix(Zombie __instance, DamageFlags theDamageFlags)
    {
        if (ReloadedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide)
                return false;

            var zombieNetworked = __instance.GetNetworked();
            if (zombieNetworked != null)
            {
                if (zombieNetworked.TryGetNetworkComponent<PogoNetworkComponent>(out var comp))
                {
                    comp.SendPogoBreakRpc(theDamageFlags);
                }
            }
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.PogoBreak))]
    internal static void PogoBreakOriginal(this Zombie __instance, DamageFlags theDamageFlags)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }
}
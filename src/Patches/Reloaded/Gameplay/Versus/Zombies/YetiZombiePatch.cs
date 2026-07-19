using HarmonyLib;
using Il2CppReloaded.Gameplay;
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
                if (comp.CurrentState != YetiNetworkComponent.YetiState.Angry)
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
}
using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class CatapultZombiePatchPatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.FindCatapultTarget))]
    [HarmonyPostfix]
    private static void Zombie_FindCatapultTarget_Postfix(Zombie __instance, ref Plant? __result)
    {
        if (ReloadedLobby.AmInLobby())
        {
            // Catapult phases are handled by ZombieNetworked.cs for non plant client
            if (!VersusState.AmPlantSide)
            {
                var zombieNetworked = __instance.GetNetworked();
                if (zombieNetworked != null)
                {
                    __result = zombieNetworked.Target;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.UpdateZombieCatapult))]
    [HarmonyPrefix]
    private static void Zombie_UpdateZombieCatapult_Prefix(Zombie __instance, ref int __state)
    {
        __state = __instance.mSummonCounter;
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.UpdateZombieCatapult))]
    [HarmonyPostfix]
    private static void Zombie_UpdateZombieCatapult_Postfix(Zombie __instance, int __state)
    {
        if (ReloadedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide)
            {
                __instance.mSummonCounter = __state;
            }
        }
    }
}

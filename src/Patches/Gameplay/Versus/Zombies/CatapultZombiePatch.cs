using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class CatapultZombiePatchPatch
{
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.FindCatapultTarget))]
    [HarmonyPostfix]
    private static void Zombie_FindCatapultTarget_Postfix(Zombie __instance, ref Plant __result)
    {
        if (ReplantedLobby.AmInLobby())
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
}

using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;

namespace ReplantedOnline.Patches.Gameplay.Versus;

[HarmonyPatch]
internal static class CurrencyProductionPatch
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.PlantInitialize))]
    [HarmonyPostfix]
    private static void Plant_PlantInitialize_Postfix(Plant __instance)
    {
        if (ReplantedLobby.AmInLobby())
        {
            if (!__instance.MakesSun()) return;

            __instance.mLaunchCounter = VersusGameplayManager.GetInitPlantOrGraveRate();
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.Update))]
    [HarmonyPrefix]
    private static void Plant_Update_Prefix(Plant __instance, ref bool __state)
    {
        __state = false;

        if (ReplantedLobby.AmInLobby())
        {
            if (!__instance.MakesSun()) return;

            if (VersusState.AmPlantSide)
            {
                if (__instance.mLaunchCounter <= 1)
                {
                    __state = true;
                }
            }
            else
            {
                __instance.mLaunchCounter = int.MaxValue;
            }
        }
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.Update))]
    [HarmonyPostfix]
    private static void Plant_Update_Postfix(Plant __instance, bool __state)
    {
        if (ReplantedLobby.AmInLobby())
        {
            if (__state)
            {
                __instance.mLaunchCounter = VersusGameplayManager.GetPlantRate();
            }
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.UpdateGravestone))]
    [HarmonyPrefix]
    private static void Zombie_UpdateGravestone_Prefix(Zombie __instance, ref bool __state)
    {
        __state = false;

        if (ReplantedLobby.AmInLobby())
        {
            if (VersusState.AmZombieSide)
            {
                // 100 is a magic number used to mark gravestone spawning brains
                if (__instance.mZombiePhase is not ((ZombiePhase)100 or (ZombiePhase)101)) return;

                if (__instance.mZombiePhase == (ZombiePhase)100)
                {
                    __instance.mZombiePhase = (ZombiePhase)101;
                    __instance.mPhaseCounter = VersusGameplayManager.GetInitPlantOrGraveRate();
                    return;
                }

                if (__instance.mPhaseCounter <= 1)
                {
                    __state = true;
                }
            }
            else
            {
                __instance.mPhaseCounter = int.MaxValue;
            }
        }
    }

    [HarmonyPatch(typeof(Zombie), nameof(Zombie.UpdateGravestone))]
    [HarmonyPostfix]
    private static void Zombie_UpdateGravestone_Postfix(Zombie __instance, bool __state)
    {
        if (ReplantedLobby.AmInLobby())
        {
            if (__state)
            {
                __instance.mPhaseCounter = VersusGameplayManager.GetGraveRate();
            }
        }
    }

    [HarmonyPatch(typeof(Board), nameof(Board.StartLevel))]
    [HarmonyPostfix]
    private static void Board_StartLevel_Postfix(Board __instance)
    {
        if (ReplantedLobby.AmInLobby())
        {
            __instance.mSunCountDown = VersusGameplayManager.GetInitSkyRate();
        }
    }

    [HarmonyPatch(typeof(Board), nameof(Board.UpdateSunSpawning))]
    [HarmonyPrefix]
    private static void Board_UpdateSunSpawning_Prefix(Board __instance, ref bool __state)
    {
        __state = false;

        if (ReplantedLobby.AmInLobby())
        {
            if (__instance.mSunCountDown <= 1)
            {
                __state = true;
            }
        }
    }

    [HarmonyPatch(typeof(Board), nameof(Board.UpdateSunSpawning))]
    [HarmonyPostfix]
    private static void Board_UpdateSunSpawning_Postfix(Board __instance, bool __state)
    {
        if (ReplantedLobby.AmInLobby())
        {
            if (__state)
            {
                __instance.mSunCountDown = VersusGameplayManager.GetSkyRate();
            }
        }
    }
}

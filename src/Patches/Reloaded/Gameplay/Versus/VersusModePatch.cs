using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Managers.Reloaded;
using ReplantedOnline.Network.Reloaded.Client;
using UnityEngine;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus;

[HarmonyPatch]
internal static class VersusModePatch
{
    [HarmonyPatch(typeof(VersusMode), nameof(VersusMode.UpdateGameplay))]
    [HarmonyPrefix]
    private static void VersusMode_UpdateGameplay_Prefix(VersusMode __instance, ref float __state)
    {
        if (__instance.m_gameplayInitialized == false)
        {
            __instance.m_gameplayInitialized = true;
            __instance.m_versusTime = -ReplantedOnlineMod.Constants.Reloaded.VERSUS_PRECOUNTDOWN_TIME;
            IArena.GetCurrentArena()?.InitializeArena(__instance);
            IVersusGamemode.GetCurrentGamemode().OnGameplayStart(__instance);
            VersusGameplayManager.OnStart(__instance);
        }

        __state = __instance.m_versusTime;
    }

    [HarmonyPatch(typeof(VersusMode), nameof(VersusMode.UpdateGameplay))]
    [HarmonyPostfix]
    private static void VersusMode_UpdateGameplay_Postfix(VersusMode __instance, float __state)
    {
        if (!ReloadedLobby.AmInLobby())
            return;

        __instance.ZombieLife = ReloadedLobby.LobbyData!.ZombieLife;

        VersusGameplayManager.SyncVersusStates(__instance, __state, __instance.m_versusTime);
        VersusGameplayManager.Update(__instance);
        IArena.GetCurrentArena()?.UpdateArena(__instance);
        IVersusGamemode.GetCurrentGamemode().UpdateGameplay(__instance);
    }

    [HarmonyPatch(typeof(VersusMode), nameof(VersusMode.UpdateWin))]
    [HarmonyPatch(typeof(VersusMode), nameof(VersusMode.InitializeWin))]
    [HarmonyPrefix]
    private static bool VersusMode_Win_Prefix()
    {
        if (ReloadedLobby.AmInLobby())
        {
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(VersusMode), nameof(VersusMode.SetFocus))]
    [HarmonyPrefix]
    private static bool VersusMode_SetFocus_Prefix()
    {
        if (ReloadedLobby.AmInLobby())
        {
            return false;
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(VersusMode), nameof(VersusMode.SetFocus))]
    internal static void SetFocusOriginal(this VersusMode __instance, GameObject focusTarget, Vector3 focusOffset)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }
}

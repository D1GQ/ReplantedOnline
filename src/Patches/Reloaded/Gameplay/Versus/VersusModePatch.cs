using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Managers.Reloaded;
using ReplantedOnline.Modules.Modded;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Client;
using UnityEngine;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.Versus;

[HarmonyPatch]
internal static class VersusModePatch
{
    [HarmonyPatch(typeof(VersusMode), nameof(VersusMode.UpdateGameplay))]
    [HarmonyPrefix]
    private static void VersusMode_UpdateGameplay_Prefix(VersusMode __instance)
    {
        if (__instance.m_gameplayInitialized == false)
        {
            __instance.m_gameplayInitialized = true;
            updateInterval.Reset();
            IArena.GetCurrentArena()?.InitializeArena(__instance);
            IVersusGamemode.GetCurrentGamemode()?.OnGameplayStart(__instance);
            VersusGameplayManager.OnStart();
        }
    }

    private readonly static ExecuteInterval updateInterval = new();
    [HarmonyPatch(typeof(VersusMode), nameof(VersusMode.UpdateGameplay))]
    [HarmonyPostfix]
    private static void VersusMode_UpdateGameplay_Postfix(VersusMode __instance)
    {
        if (!ReloadedLobby.AmInLobby()) return;

        __instance.ZombieLife = ReloadedLobby.LobbyData.ZombieLife;

        if (updateInterval.Execute())
        {
            IArena.GetCurrentArena()?.UpdateArena(__instance);
            IVersusGamemode.GetCurrentGamemode()?.UpdateGameplay(__instance);
        }
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

    [HarmonyPatch(typeof(VersusMode), nameof(VersusMode.UpdateBobsledSpawning))]
    [HarmonyPrefix]
    private static bool VersusMode_UpdateBobsledSpawning_Prefix()
    {
        // Only apply these changes when in an online lobby
        if (ReloadedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide)
            {
                return false;
            }
        }

        return true;
    }
}

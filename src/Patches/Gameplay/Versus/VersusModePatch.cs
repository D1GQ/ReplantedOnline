using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Exceptions;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using UnityEngine;

namespace ReplantedOnline.Patches.Gameplay.Versus;

[HarmonyPatch]
internal static class VersusModePatch
{
    [HarmonyPatch(typeof(VersusMode), nameof(VersusMode.InitializeGameplay))]
    [HarmonyPrefix]
    private static void VersusMode_InitializeGameplay_Prefix(VersusMode __instance)
    {
        updateInterval.Reset();
        __instance.m_app.BackgroundController.EnableBowlingLine(true, 515);
        __instance.ClearBoard();
        IArena.GetCurrentArena()?.InitializeArena(__instance);
        IVersusGamemode.GetCurrentGamemode()?.OnGameplayStart(__instance);
        VersusGameplayManager.OnStart();

        // Prevent initial plants and zombies from being placed
        throw new SilentPatchException();
    }

    private readonly static ExecuteInterval updateInterval = new();
    [HarmonyPatch(typeof(VersusMode), nameof(VersusMode.UpdateGameplay))]
    [HarmonyPostfix]
    private static void VersusMode_UpdateGameplay_Postfix(VersusMode __instance)
    {
        if (!ReplantedLobby.AmInLobby()) return;

        __instance.ZombieLife = ReplantedLobby.LobbyData.ZombieLife;

        if (updateInterval.Execute())
        {
            IArena.GetCurrentArena()?.UpdateArena(__instance);
            IVersusGamemode.GetCurrentGamemode()?.UpdateGameplay(__instance);
        }
    }

    [HarmonyPatch(typeof(VersusMode), nameof(VersusMode.SetFocus))]
    [HarmonyPrefix]
    private static bool VersusMode_SetFocus_Prefix()
    {
        if (ReplantedLobby.AmInLobby())
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
        if (ReplantedLobby.AmInLobby())
        {
            if (!VersusState.AmPlantSide)
            {
                return false;
            }
        }

        return true;
    }
}

using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Managers.Reloaded;
using ReplantedOnline.Modules.Modded;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Network.Reloaded.Client.Routing;
using ReplantedOnline.Network.Reloaded.Client.Routing.Packet;
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
            updateInterval.Reset();
            IArena.GetCurrentArena()?.InitializeArena(__instance);
            IVersusGamemode.GetCurrentGamemode().OnGameplayStart(__instance);
            VersusGameplayManager.OnStart();
        }

        __state = __instance.m_versusTime;
    }

    private readonly static ExecuteInterval updateInterval = new();
    [HarmonyPatch(typeof(VersusMode), nameof(VersusMode.UpdateGameplay))]
    [HarmonyPostfix]
    private static void VersusMode_UpdateGameplay_Postfix(VersusMode __instance, float __state)
    {
        if (!ReloadedLobby.AmInLobby()) return;

        __instance.ZombieLife = ReloadedLobby.LobbyData!.ZombieLife;

        if (updateInterval.Execute())
        {
            IArena.GetCurrentArena()?.UpdateArena(__instance);
            IVersusGamemode.GetCurrentGamemode().UpdateGameplay(__instance);
            VersusGameplayManager.Update();
        }

        if (ReloadedLobby.AmLobbyHost())
        {
            if ((int)(__instance.m_versusTime * 2f) != (int)(__state * 2f))
            {
                NetworkManager.Packet<SyncVersusTimePacket>.Singleton.Send(__instance.m_versusTime);
            }
        }
        else
        {
            __instance.m_versusTime = __state;
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
}

using HarmonyLib;
using Il2CppReloaded.Services;
using Il2CppReloaded.TreeStateActivities;
using ReplantedOnline.Network.Online;
using static ReplantedOnline.Managers.BloomEngineManager;

namespace ReplantedOnline.Patches.Gameplay.Versus;

[HarmonyPatch]
internal static class MusicActivityPatch
{
    [HarmonyPatch(typeof(MusicActivity), nameof(MusicActivity.TriggerAudio))]
    [HarmonyPrefix]
    private static void MusicActivity_TriggerAudio_Prefix(MusicActivity __instance, ref MusicTune? __state)
    {
        // Initialize state variable
        __state = null;

        // Check if this is the "StartMatch" activity
        if (__instance.name == "StartMatch")
        {
            // Check if in multiplayer lobby and music modification is enabled
            if (NetLobby.AmInLobby() && BloomConfigs.ModifyMusic.Value)
            {
                // Save original music tune to state
                __state = __instance.m_musicTune;

                // Replace with custom multiplayer music
                __instance.m_musicTune = MusicTune.MinigameLoonboon;
            }
        }
    }

    [HarmonyPatch(typeof(MusicActivity), nameof(MusicActivity.TriggerAudio))]
    [HarmonyPostfix]
    private static void MusicActivity_TriggerAudio_Postfix(MusicActivity __instance, MusicTune? __state)
    {
        // Check if this was the "StartMatch" activity
        if (__instance.name == "StartMatch")
        {
            // Check if we saved an original music tune
            if (__state != null)
            {
                // Restore original music tune
                __instance.m_musicTune = __state.Value;
            }
        }
    }
}
using HarmonyLib;
using Il2CppReloaded.Characters;
using ReplantedOnline.Helper;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Steam;

namespace ReplantedOnline.Patches.Gameplay.Versus.Networked;

[HarmonyPatch]
internal static class AnimationControllerSyncPatch
{
    [HarmonyPatch(typeof(CharacterAnimationController), nameof(CharacterAnimationController.PlayAnimation))]
    [HarmonyPrefix]
    private static bool CharacterAnimationController_PlayAnimation_Prefix(CharacterAnimationController __instance, string animationName, CharacterTracks track, float fps, AnimLoopType loopType)
    {
        if (NetLobby.AmInLobby())
        {
            var netAnimationController = __instance.GetNetworked<AnimationControllerNetworked>();
            if (netAnimationController != null && netAnimationController.DoSendAnimate())
            {
                netAnimationController.SendPlayAnimationRpc(animationName, track, fps, loopType);
            }
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(CharacterAnimationController), nameof(CharacterAnimationController.PlayAnimation))]
    internal static void PlayAnimationOriginal(this CharacterAnimationController __instance, string animationName, CharacterTracks track, float fps, AnimLoopType loopType)
    {
        throw new NotImplementedException("Reverse Patch Stub");
    }
}
using HarmonyLib;
using Il2CppReloaded.Characters;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Patches.Versus.NetworkSync;

[HarmonyPatch]
internal static class AnimationControllerSyncPatch
{
    [HarmonyPatch(typeof(CharacterAnimationController), nameof(CharacterAnimationController.PlayAnimation))]
    [HarmonyPrefix]
    private static bool PlayAnimation_Prefix(CharacterAnimationController __instance, string animationName, CharacterTracks track, float fps, AnimLoopType loopType)
    {
        if (InternalCallContext.IsInternalCall_PlayAnimation) return true;

        if (NetLobby.AmInLobby())
        {
            var netAnimationController = __instance.GetNetworked<AnimationControllerNetworked>();
            if (netAnimationController != null)
            {
                if (netAnimationController.ParentNetworkClass is PlantNetworked plantNet)
                {
                    if (plantNet._Plant.mSeedType is (SeedType.Squash or SeedType.Chomper or SeedType.Potatomine))
                    {
                        if (!netAnimationController.AmOwner) return false;

                        netAnimationController.SendPlayAnimationRpc(animationName, track, fps, loopType);
                        __instance.PlayAnimationOriginal(animationName, track, fps, loopType);

                        return false;
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Extension method that safely calls the original PlayAnimation method
    /// while preventing our patch from intercepting the call (avoiding recursion)
    /// </summary>
    internal static void PlayAnimationOriginal(this CharacterAnimationController __instance, string animationName, CharacterTracks track, float fps, AnimLoopType loopType)
    {
        InternalCallContext.IsInternalCall_PlayAnimation = true;
        try
        {
            __instance.PlayAnimation(animationName, track, fps, loopType);
        }
        finally
        {
            // Always reset the flag, even if an exception occurs
            InternalCallContext.IsInternalCall_PlayAnimation = false;
        }
    }

    /// <summary>
    /// Thread-safe context flags to prevent infinite recursion when calling patched methods from within patches.
    /// [ThreadStatic] ensures each thread has its own copy of these flags.
    /// </summary>
    private static class InternalCallContext
    {
        [ThreadStatic]
        public static bool IsInternalCall_PlayAnimation;
    }
}
using Il2CppReloaded.Characters;
using ReplantedOnline.Attributes;
using ReplantedOnline.Monos;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Network.Client.Object.Replanted;

/// <summary>
/// Represents a networked animation controller for synchronizing character animations across the network.
/// </summary>
internal sealed class AnimationControllerNetworked : NetworkObject
{
    internal enum AnimationRpcs
    {
        PlayAnimation
    }

    internal CharacterAnimationController _AnimationController;

    internal void Init(CharacterAnimationController animationController)
    {
        _AnimationController = animationController;
        _AnimationController.AddNetworkedLookup(this);
        var observable = _AnimationController.gameObject.AddComponent<ObservableGameObject>();
        observable.OnGameObjectDestroy += Observable_OnGameObjectDestroy;
    }

    private void Observable_OnGameObjectDestroy(UnityEngine.GameObject obj)
    {
        _AnimationController.RemoveNetworkedLookup();
    }

    private void OnDestroy()
    {
        _AnimationController.RemoveNetworkedLookup();
    }

    internal bool DoSendAnimate()
    {
        if (!AmOwner) return false;

        return false;
    }

    internal void SendPlayAnimationRpc(string animationName, CharacterTracks track, float fps, AnimLoopType loopType)
    {
        SendNetworkObjectRpc(AnimationRpcs.PlayAnimation, animationName, track, fps, loopType);
    }

    [RpcHandler(AnimationRpcs.PlayAnimation)]
    internal void HandlePlayAnimationRpc(string animationName, CharacterTracks track, float fps, AnimLoopType loopType)
    {
        _AnimationController?.PlayAnimationOriginal(animationName, track, fps, loopType);
    }
}

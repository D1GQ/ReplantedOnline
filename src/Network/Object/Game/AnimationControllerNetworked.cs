using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Characters;
using ReplantedOnline.Helper;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Versus.NetworkSync;

namespace ReplantedOnline.Network.Object.Game;

/// <summary>
/// Represents a networked animation controller for synchronizing character animations across the network.
/// </summary>
internal class AnimationControllerNetworked : NetworkClass
{
    private CharacterAnimationController _AnimationController;

    internal void Init(CharacterAnimationController animationController)
    {
        _AnimationController = animationController;
        _AnimationController.AddNetworkedLookup(this);
    }

    public void OnDestroy()
    {
        _AnimationController?.RemoveNetworkedLookup();
    }

    [HideFromIl2Cpp]
    public override void HandleRpc(SteamNetClient sender, byte rpcId, PacketReader packetReader)
    {
        if (sender.SteamId != OwnerId) return;

        switch (rpcId)
        {
            case 0:
                {
                    var animationName = packetReader.ReadString();
                    var track = (CharacterTracks)packetReader.ReadInt();
                    var fps = packetReader.ReadFloat();
                    var loopType = (AnimLoopType)packetReader.ReadByte();
                    HandlePlayAnimationRpc(animationName, track, fps, loopType);
                }
                break;
        }
    }

    internal void SendPlayAnimationRpc(string animationName, CharacterTracks track, float fps, AnimLoopType loopType)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteString(animationName);
        packetWriter.WriteInt((int)track);
        packetWriter.WriteFloat(fps);
        packetWriter.WriteByte((byte)loopType);
        this.SendRpc(0, packetWriter);
    }

    private void HandlePlayAnimationRpc(string animationName, CharacterTracks track, float fps, AnimLoopType loopType)
    {
        _AnimationController?.PlayAnimationOriginal(animationName, track, fps, loopType);
    }
}

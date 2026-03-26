using Il2CppReloaded.Characters;
using ReplantedOnline.Attributes;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client.Object.Replanted;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Network.Server.Packet.FastResolvers;

[RegisterFastPacketResolver]
internal class CharacterAnimationControllerResolver : IFastPacketResolver<CharacterAnimationController>
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type == typeof(CharacterAnimationController);

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter, CharacterAnimationController value)
    {
        if (value != null)
        {
            var netAnimationController = value.GetNetworked<AnimationControllerNetworked>();
            packetWriter.WriteNetworkObject(netAnimationController);
        }
        else
        {
            packetWriter.WriteNetworkObject(null);
        }
    }

    /// <inheritdoc/>
    public CharacterAnimationController Deserialize(PacketReader packetReader, Type type)
    {
        var netAnimationController = packetReader.ReadNetworkObject<AnimationControllerNetworked>();
        if (netAnimationController != null)
        {
            return netAnimationController._AnimationController;
        }
        else
        {
            return null;
        }
    }
}
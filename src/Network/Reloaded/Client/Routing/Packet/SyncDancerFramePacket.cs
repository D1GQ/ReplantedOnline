using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Managers.Reloaded;
using ReplantedOnline.Network.Reloaded.Serialization;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacket(PacketType.SyncDancerFrame, false)]
internal sealed class SyncDancerFramePacket : IPacketMessage<bool>
{
    /// <inheritdoc/>
    public void Send(bool isDancing)
    {
        PacketWriter packetWriter = NetworkManager.StartPacket(PacketType.SyncDancerFrame);
        packetWriter.WriteBool(isDancing);
        NetworkManager.EndPacketAndSend(packetWriter, PacketChannel.Buffered, false, false);
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        if (!sender.AmHost)
            return;

        VersusGameplayManager.IsDancingThisFrameSynced = packetReader.ReadBool();
    }
}

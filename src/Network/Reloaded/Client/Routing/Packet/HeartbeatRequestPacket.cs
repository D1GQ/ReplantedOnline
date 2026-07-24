using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Serialization;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacket(PacketType.HeartbeatRequest, false)]
internal sealed class HeartbeatRequestPacket : IPacketMessage<uint>
{
    /// <inheritdoc/>
    public void Send(uint timeStamp)
    {
        PacketWriter packetWriter = PacketWriter.Get();
        packetWriter.WriteUInt(timeStamp);
        NetworkManager.SendPacket(packetWriter, PacketType.HeartbeatRequest, PacketChannel.Buffered, false, false);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        uint timeStamp = packetReader.ReadUInt();
        NetworkManager.Packet<HeartbeatPacket>.Singleton.Send(sender.ClientId, timeStamp);
    }
}

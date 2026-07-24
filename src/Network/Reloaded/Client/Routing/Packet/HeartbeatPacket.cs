using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Structs.Network;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacket(PacketType.Heartbeat, false)]
internal sealed class HeartbeatPacket : IPacketMessage<ID, uint>
{
    /// <inheritdoc/>
    public void Send(ID targetId, uint timeStamp)
    {
        PacketWriter packetWriter = PacketWriter.Get();
        packetWriter.WriteUInt(timeStamp);
        NetworkManager.SendPacketTo(targetId, packetWriter, PacketType.Heartbeat, PacketChannel.Buffered, false);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        uint timeStamp = packetReader.ReadUInt();
        NetworkManager.NetworkHeartbeat.HandleHeartbeat(sender, timeStamp);
    }
}

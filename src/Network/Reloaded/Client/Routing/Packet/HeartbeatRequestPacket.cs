using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Serialization;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacket(PacketType.HeartbeatRequest, false)]
internal sealed class HeartbeatRequestPacket : IPacketMessage<ulong>
{
    /// <inheritdoc/>
    public void Send(ulong timeStamp)
    {
        PacketWriter packetWriter = PacketWriter.Get();
        packetWriter.WriteULong(timeStamp);
        NetworkManager.SendPacket(packetWriter, PacketType.HeartbeatRequest, PacketChannel.Rpc, false, false);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        ulong timeStamp = packetReader.ReadULong();
        NetworkManager.Packet<HeartbeatPacket>.Singleton.Send(sender.ClientId, timeStamp);
    }
}

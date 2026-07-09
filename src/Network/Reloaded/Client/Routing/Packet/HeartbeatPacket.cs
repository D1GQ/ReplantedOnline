using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Structs.Network;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacket(PacketType.Heartbeat, false)]
internal sealed class HeartbeatPacket : IPacketMessage<ID, ulong>
{
    /// <inheritdoc/>
    public void Send(ID targetId, ulong timeStamp)
    {
        PacketWriter packetWriter = PacketWriter.Get();
        packetWriter.WriteULong(timeStamp);
        NetworkManager.SendPacketTo(targetId, packetWriter, PacketType.Heartbeat, PacketChannel.Rpc, false);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        ulong timeStamp = packetReader.ReadULong();
        NetworkManager.NetworkHeartbeat.HandleHeartbeat(sender, timeStamp);
    }
}

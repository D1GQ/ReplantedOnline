using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Reloaded.Serialization;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacket(PacketType.SyncVersusTime, false)]
internal sealed class SyncVersusTimePacket : IPacketMessage<float>
{
    /// <inheritdoc/>
    public void Send(float time)
    {
        PacketWriter packetWriter = NetworkManager.StartPacket(PacketType.SyncVersusTime);
        packetWriter.WriteFloat(time);
        NetworkManager.EndPacketAndSend(packetWriter, PacketChannel.Buffered, false, false);
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        if (!sender.AmHost)
            return;

        float time = packetReader.ReadFloat();
        Instances.GameplayActivity.VersusMode.m_versusTime = time;
    }
}

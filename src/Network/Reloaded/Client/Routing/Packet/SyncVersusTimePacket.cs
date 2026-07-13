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
        PacketWriter packetWriter = PacketWriter.Get();
        packetWriter.WriteFloat(time);
        NetworkManager.SendPacket(packetWriter, PacketType.SyncVersusTime, PacketChannel.Rpc, false, false);
        packetWriter.Recycle();
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

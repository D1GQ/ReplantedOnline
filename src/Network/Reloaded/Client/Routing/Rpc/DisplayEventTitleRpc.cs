using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Reloaded.Client.Routing.Packet;
using ReplantedOnline.Network.Reloaded.Serialization;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Rpc;

[RegisterRpc(RpcType.DisplayEventTitleRpc)]
internal sealed class DisplayEventTitleRpc : IRpcMessage<ArenaEvents.EventTitle>
{
    /// <inheritdoc/>
    public void Send(ArenaEvents.EventTitle title)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteEnum(title);
        NetworkManager.Packet<RpcPacket>.Singleton.Send(RpcType.DisplayEventTitleRpc, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader)
    {
        if (sender.AmHost)
        {
            var title = packetReader.ReadEnum<ArenaEvents.EventTitle>();
            ArenaEvents.DisplayEventTitle(title);
        }
    }
}
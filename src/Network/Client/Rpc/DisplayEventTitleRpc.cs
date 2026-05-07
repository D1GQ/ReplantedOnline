using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Routing;

namespace ReplantedOnline.Network.Client.Rpc;

[RegisterRpc]
internal sealed class DisplayEventTitleRpc : IRpcDispatcher<ArenaEvents.EventTitle>
{
    /// <inheritdoc/>
    public RpcType Rpc => RpcType.DisplayEventTitleRpc;

    /// <inheritdoc/>
    public void Send(ArenaEvents.EventTitle title)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteEnum(title);
        NetworkDispatcher.SendRpc(Rpc, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Handle(ReplantedClientData sender, PacketReader packetReader)
    {
        if (sender.AmHost)
        {
            var title = packetReader.ReadEnum<ArenaEvents.EventTitle>();
            ArenaEvents.DisplayEventTitle(title);
        }
    }
}
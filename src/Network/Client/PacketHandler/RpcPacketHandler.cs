using ReplantedOnline.Attributes.Modded;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Packet.Messages;
using ReplantedOnline.Utilities.MelonLoader;

namespace ReplantedOnline.Network.Client.PacketHandler;

[RegisterPacketHandler(PacketHandlerType.Rpc)]
internal sealed class RpcPacketHandler : IPacketHandler
{
    /// <inheritdoc/>
    public void Handle(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        var message = Message<RpcMessage>.Instance.Deserialize(packetReader);
        if (!local)
        {
            ReplantedOnlineMod.Logger.Msg(typeof(RpcPacketHandler), $"Processing RPC from {sender.Name}: {message.RpcType}");
        }
        IRpc.HandleRpc(message.RpcType, sender, packetReader);
    }
}

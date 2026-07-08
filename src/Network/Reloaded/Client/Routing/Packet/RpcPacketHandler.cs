using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Network.Reloaded.Serialization.Messages;
using ReplantedOnline.Utilities.MelonLoader;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacketHandler(PacketType.Rpc)]
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

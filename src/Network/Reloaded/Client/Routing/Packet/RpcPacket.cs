using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Network.Reloaded.Serialization.Messages;
using ReplantedOnline.Utilities.MelonLoader;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacketHandler(PacketType.Rpc)]
internal sealed class RpcPacket : IPacketMessage<RpcType, IPacket?, bool>
{
    /// <inheritdoc/>
    public void Send(RpcType rpc, IPacket? payload = null, bool receiveLocally = false)
    {
        PacketWriter packetWriter = PacketWriter.Get();
        Message<RpcMessage>.Singleton.Serialize(packetWriter, rpc);
        if (payload != null)
        {
            packetWriter.WritePacketToBuffer(payload);
        }

        ReplantedOnlineMod.Logger.Msg(typeof(NetworkManager), $"Sent RPC: {Enum.GetName(rpc)}");
        NetworkManager.SendPacket(packetWriter, PacketType.Rpc, PacketChannel.Rpc, receiveLocally);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        var message = Message<RpcMessage>.Singleton.Deserialize(packetReader);
        if (!local)
        {
            ReplantedOnlineMod.Logger.Msg(typeof(RpcPacket), $"Processing RPC from {sender.Name}: {message.RpcType}");
        }
        IBaseRpcMessage.HandleRpc(message.RpcType, sender, packetReader);
    }
}

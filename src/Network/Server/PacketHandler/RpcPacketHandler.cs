using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Server.Packet;

namespace ReplantedOnline.Network.Server.PacketHandler;

[RegisterPacketHandler]
internal sealed class RpcPacketHandler : IPacketHandler
{
    /// <inheritdoc/>
    public PacketTag Tag => PacketTag.Rpc;

    /// <inheritdoc/>
    public void Handle(NetClient sender, PacketReader packetReader)
    {
        ClientRpcType rpc = (ClientRpcType)packetReader.ReadByte();
        ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Processing RPC from {sender.Name}: {rpc}");
        IClientRPC.HandleRpc(rpc, sender, packetReader);
    }
}

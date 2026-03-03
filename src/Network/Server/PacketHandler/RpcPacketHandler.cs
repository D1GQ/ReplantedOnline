using MelonLoader;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Server.ClientRPC;
using ReplantedOnline.Network.Server.Packet;

namespace ReplantedOnline.Network.Server.PacketHandler;

[RegisterPacketHandler]
internal sealed class RpcPacketHandler : BasePacketHandler
{
    /// <inheritdoc/>
    internal sealed override PacketTag Tag => PacketTag.Rpc;

    /// <inheritdoc/>
    internal sealed override void Handle(NetClient sender, PacketReader packetReader)
    {
        ClientRpcType rpc = (ClientRpcType)packetReader.ReadByte();
        MelonLogger.Msg($"[NetworkDispatcher] Processing RPC from {sender.Name}: {rpc}");
        BaseClientRPC.HandleRpc(rpc, sender, packetReader);
    }
}

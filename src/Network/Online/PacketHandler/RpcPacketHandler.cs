using MelonLoader;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Network.Online.ClientRPC;
using ReplantedOnline.Network.Packet;

namespace ReplantedOnline.Network.Online.PacketHandler;

[RegisterPacketHandler]
internal sealed class RpcPacketHandler : BasePacketHandler
{
    /// <inheritdoc/>
    internal sealed override PacketTag Tag => PacketTag.Rpc;

    /// <inheritdoc/>
    internal sealed override void Handle(SteamNetClient sender, PacketReader packetReader)
    {
        ClientRpcType rpc = (ClientRpcType)packetReader.ReadByte();
        MelonLogger.Msg($"[NetworkDispatcher] Processing RPC from {sender.Name}: {rpc}");
        BaseClientRPC.HandleRpc(rpc, sender, packetReader);
    }
}

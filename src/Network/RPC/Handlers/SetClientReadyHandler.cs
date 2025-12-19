using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Managers;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Packet;

namespace ReplantedOnline.Network.RPC.Handlers;

[RegisterRPCHandler]
internal sealed class SetClientReadyHandler : RPCHandler
{
    /// <inheritdoc/>
    internal sealed override RpcType Rpc => RpcType.SetClientReady;

    internal static void Send()
    {
        SteamNetClient.LocalClient.Ready = true;
        NetworkDispatcher.SendRpc(RpcType.SetClientReady);
    }

    /// <inheritdoc/>
    internal sealed override void Handle(SteamNetClient sender, PacketReader packetReader)
    {
        sender.Ready = true;
        VersusManager.UpdateSideVisuals();
    }
}

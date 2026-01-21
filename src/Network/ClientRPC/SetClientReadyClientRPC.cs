using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Managers;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Packet;

namespace ReplantedOnline.Network.ClientRPC;

[RegisterRPCHandler]
internal sealed class SetClientReadyClientRPC : BaseClientRPCHandler
{
    /// <inheritdoc/>
    internal sealed override ClientRpcType Rpc => ClientRpcType.SetClientReady;

    internal static void Send()
    {
        SteamNetClient.LocalClient.Ready = true;
        NetworkDispatcher.SendRpc(ClientRpcType.SetClientReady);
    }

    /// <inheritdoc/>
    internal sealed override void Handle(SteamNetClient sender, PacketReader packetReader)
    {
        sender.Ready = true;
        VersusLobbyManager.UpdateSideVisuals();
    }
}

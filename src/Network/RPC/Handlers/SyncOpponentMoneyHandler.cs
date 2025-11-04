using ReplantedOnline.Helper;
using ReplantedOnline.Items.Enums;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Packet;

namespace ReplantedOnline.Network.RPC.Handlers;

internal class SyncOpponentMoneyHandler : RPCHandler
{
    /// <inheritdoc/>
    internal sealed override RpcType Rpc => RpcType.SyncOpponentMoney;

    internal static void Send(int current, int amount)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteInt(current);
        packetWriter.WriteInt(amount);
        NetworkDispatcher.SendRpc(RpcType.SyncOpponentMoney, packetWriter);
    }

    /// <inheritdoc/>
    internal sealed override void Handle(SteamNetClient sender, PacketReader packetReader)
    {
        var current = packetReader.ReadInt();
        var amount = packetReader.ReadInt();
        Utils.SyncPlayerMoney(ReplantedOnlineMod.Constants.OPPONENT_PLAYER_INDEX, current, amount);
    }
}

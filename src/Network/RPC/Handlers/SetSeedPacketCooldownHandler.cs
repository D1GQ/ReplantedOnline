using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Items.Enums;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Packet;

namespace ReplantedOnline.Network.RPC.Handlers;

internal class SetSeedPacketCooldownHandler : RPCHandler
{
    /// <inheritdoc/>
    internal sealed override RpcType Rpc => RpcType.SetSeedPacketCooldown;

    internal static void Send(SeedType seedType)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteInt((int)seedType);
        NetworkDispatcher.SendRpc(RpcType.SetSeedPacketCooldown, packetWriter);
    }

    /// <inheritdoc/>
    internal sealed override void Handle(SteamNetClient sender, PacketReader packetReader)
    {
        var seedType = (SeedType)packetReader.ReadInt();
        Utils.SetSeedPacketCooldown(ReplantedOnlineMod.Constants.OPPONENT_PLAYER_INDEX, seedType);
    }
}

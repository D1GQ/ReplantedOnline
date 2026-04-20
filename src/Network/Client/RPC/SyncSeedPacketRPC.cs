using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Network.Client.RPC;

[RegisterRPC]
internal sealed class SyncSeedPacketRPC : IRPC
{
    /// <inheritdoc/>
    public RpcType Type => RpcType.SyncSeedPacket;

    internal static void Send(SeedType seedType)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteInt((int)seedType);
        NetworkDispatcher.SendRpc(RpcType.SyncSeedPacket, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Handle(ReplantedClientData sender, PacketReader packetReader)
    {
        // Read the seed type from the packet
        var seedType = (SeedType)packetReader.ReadInt();
        var seedPacket = Instances.GameplayActivity.Board.SeedBanks.OpponentItem().SeedPackets.FirstOrDefault(packet => packet.mPacketType == seedType);

        if (seedPacket != null)
        {
            seedPacket.WasPlanted(ReplantedOnlineMod.Constants.OPPONENT_PLAYER_INDEX);
            seedPacket.mActive = false;
        }
    }
}

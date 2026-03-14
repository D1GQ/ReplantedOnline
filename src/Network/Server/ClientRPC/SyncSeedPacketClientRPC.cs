using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Server.Packet;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Network.Server.ClientRPC;

[RegisterClientRPC]
internal sealed class SyncSeedPacketClientRPC : IClientRPC
{
    /// <inheritdoc/>
    public ClientRpcType Rpc => ClientRpcType.SyncSeedPacket;

    internal static void Send(SeedType seedType)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteInt((int)seedType);
        NetworkDispatcher.SendRpc(ClientRpcType.SyncSeedPacket, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Handle(NetClient sender, PacketReader packetReader)
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

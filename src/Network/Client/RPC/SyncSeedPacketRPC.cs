using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Routing;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Network.Client.Rpc;

[RegisterRpc]
internal sealed class SyncSeedPacketRpc : IRpcDispatcher<SeedType>
{
    /// <inheritdoc/>
    public RpcType Rpc => RpcType.SyncSeedPacket;

    /// <inheritdoc/>
    public void Send(SeedType seedType)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteEnum(seedType);
        NetworkDispatcher.SendRpc(Rpc, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Handle(ReplantedClientData sender, PacketReader packetReader)
    {
        // Read the seed type from the packet
        var seedType = packetReader.ReadEnum<SeedType>();
        var seedPacket = Instances.GameplayActivity.Board.SeedBanks.OpponentItem().SeedPackets.FirstOrDefault(packet => packet.mPacketType == seedType);

        if (seedPacket != null)
        {
            seedPacket.WasPlanted(ReplantedOnlineMod.Constants.OPPONENT_PLAYER_INDEX);
            seedPacket.mActive = false;
        }
    }
}

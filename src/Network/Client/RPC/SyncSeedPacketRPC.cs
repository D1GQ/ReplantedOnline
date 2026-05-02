using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus;
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
        packetWriter.WriteInt(Instances.GameplayActivity.Board.SeedBanks.LocalItem().SeedPackets.First(packet => packet.mPacketType == seedType).Index);
        NetworkDispatcher.SendRpc(Rpc, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Handle(ReplantedClientData sender, PacketReader packetReader)
    {
        // Read the seed type from the packet
        var seedType = packetReader.ReadEnum<SeedType>();
        var seedIndex = packetReader.ReadInt();
        if (Instances.GameplayActivity.Board.SeedBanks.OpponentItem().SeedPackets.Count < seedIndex) return;
        var seedPacket = Instances.GameplayActivity.Board.SeedBanks.OpponentItem().SeedPackets[seedIndex];

        if (seedPacket != null)
        {
            if (seedPacket.mPacketType == SeedPacketDefinitions.HiddenSeed)
            {
                seedPacket.PacketType = seedType;
                seedPacket.mActive = true;
                seedPacket.mActive = false;
            }
            seedPacket.WasPlanted(ReplantedOnlineMod.Constants.OPPONENT_PLAYER_INDEX);
            seedPacket.mRefreshTime = VersusGameplayManager.GetSeedPacketRefreshTime(seedType);
            seedPacket.mActive = false;
        }
    }
}

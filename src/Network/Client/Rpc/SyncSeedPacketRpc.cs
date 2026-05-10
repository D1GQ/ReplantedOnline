using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Managers.Reloaded;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Routing;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Network.Client.Rpc;

[RegisterRpc(RpcType.SyncSeedPacket)]
internal sealed class SyncSeedPacketRpc : IRpcDispatcher<SeedType>
{
    /// <inheritdoc/>
    public void Send(SeedType seedType)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteEnum(seedType);
        packetWriter.WriteInt(Instances.GameplayActivity.Board.SeedBanks.LocalItem().SeedPackets.First(packet => packet.mPacketType == seedType).Index);
        NetworkDispatcher.SendRpc(RpcType.SyncSeedPacket, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Handle(ReloadedClientData sender, PacketReader packetReader)
    {
        // Read the seed type from the packet
        var seedType = packetReader.ReadEnum<SeedType>();
        var seedIndex = packetReader.ReadInt();
        if (Instances.GameplayActivity.Board.SeedBanks.OpponentItem().SeedPackets.Count < seedIndex) return;
        var seedPacket = Instances.GameplayActivity.Board.SeedBanks.OpponentItem().SeedPackets[seedIndex];

        if (seedPacket != null)
        {
            if (seedPacket.mPacketType == SeedPacketDefinitions.RandomHiddenSeed)
            {
                seedPacket.PacketType = seedType;
                seedPacket.mActive = true;
                seedPacket.mActive = false;
            }
            seedPacket.WasPlanted(ReplantedOnlineMod.Constants.Reloaded.OPPONENT_PLAYER_INDEX);
            seedPacket.mRefreshTime = VersusGameplayManager.GetSeedPacketRefreshTime(seedType);
            seedPacket.mActive = false;
        }
    }
}

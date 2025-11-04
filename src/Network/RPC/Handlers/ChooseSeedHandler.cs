using Il2CppReloaded.Gameplay;
using ReplantedOnline.Items.Enums;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Versus.NetworkSync;

namespace ReplantedOnline.Network.RPC.Handlers;

internal class ChooseSeedHandler : RPCHandler
{
    /// <inheritdoc/>
    internal sealed override RpcType Rpc => RpcType.ChooseSeed;

    internal static void Send(ChosenSeed theChosenSeed)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteInt((int)theChosenSeed.mSeedType);
        NetworkDispatcher.SendRpc(RpcType.ChooseSeed, packetWriter);
    }

    /// <inheritdoc/>
    internal sealed override void Handle(SteamNetClient sender, PacketReader packetReader)
    {
        // Read the chosen seed type from the packet
        var seedType = (SeedType)packetReader.ReadInt();
        var SeedChooserScreen = Instances.GameplayDataProvider.m_gameplayDataModel.m_seedChooserDataModel.m_seedChooserScreen;
        var theChosenSeed = SeedChooserScreen.GetChosenSeedFromType(seedType);

        // Use player index 1 (opposite player) when choosing seed for remote player
        if (VersusState.PlantSide)
        {
            if (Instances.GameplayActivity.VersusMode.Phase is VersusPhase.ChoosePlantPacket)
            {
                SeedChooserScreen.ClickedSeedInChooserOriginal(theChosenSeed, ReplantedOnlineMod.Constants.OPPONENT_PLAYER_INDEX);
            }
        }
        else
        {
            if (Instances.GameplayActivity.VersusMode.Phase is VersusPhase.ChooseZombiePacket)
            {
                SeedChooserScreen.ClickedSeedInChooserOriginal(theChosenSeed, ReplantedOnlineMod.Constants.OPPONENT_PLAYER_INDEX);
            }
        }
    }
}

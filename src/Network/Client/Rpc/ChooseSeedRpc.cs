using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Modded;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Routing;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Networked;

namespace ReplantedOnline.Network.Client.Rpc;

[RegisterRpc(RpcType.ChooseSeed)]
internal sealed class ChooseSeedRpc : IRpcDispatcher<ChosenSeed>
{
    /// <inheritdoc/>
    public void Send(ChosenSeed theChosenSeed)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteEnum(theChosenSeed.mSeedType);
        NetworkDispatcher.SendRpc(RpcType.ChooseSeed, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Handle(ReloadedClientData sender, PacketReader packetReader)
    {
        // Read the chosen seed type from the packet
        var seedType = packetReader.ReadEnum<SeedType>();
        var SeedChooserScreen = Instances.GameplayActivity.SeedChooserScreen;
        var theChosenSeed = SeedChooserScreen.GetChosenSeedFromType(seedType);

        // Use player index 1 (opposite player) when choosing seed for remote player
        SeedChooserScreen.ClickedSeedInChooserOriginal(theChosenSeed, ReplantedOnlineMod.Constants.Reloaded.OPPONENT_PLAYER_INDEX);
    }
}

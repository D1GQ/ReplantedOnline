using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Routing;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;

namespace ReplantedOnline.Network.Client.Rpc;

[RegisterRpc]
internal sealed class ChooseSeedRpc : IRpcDispatcher<ChosenSeed>
{
    /// <inheritdoc/>
    public RpcType Rpc => RpcType.ChooseSeed;

    /// <inheritdoc/>
    public void Send(ChosenSeed theChosenSeed)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteEnum(theChosenSeed.mSeedType);
        NetworkDispatcher.SendRpc(Rpc, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Handle(ReplantedClientData sender, PacketReader packetReader)
    {
        // Read the chosen seed type from the packet
        var seedType = packetReader.ReadEnum<SeedType>();
        var SeedChooserScreen = Instances.GameplayActivity.SeedChooserScreen;
        var theChosenSeed = SeedChooserScreen.GetChosenSeedFromType(seedType);

        // Use player index 1 (opposite player) when choosing seed for remote player
        SeedChooserScreen.ClickedSeedInChooserOriginal(theChosenSeed, ReplantedOnlineMod.Constants.OPPONENT_PLAYER_INDEX);
    }
}

using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Routing;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;

namespace ReplantedOnline.Network.Client.RPC;

[RegisterRPC]
internal sealed class ChooseSeedRPC : IRPC
{
    /// <inheritdoc/>
    public RpcType Type => RpcType.ChooseSeed;

    internal static void Send(ChosenSeed theChosenSeed)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteInt((int)theChosenSeed.mSeedType);
        NetworkDispatcher.SendRpc(RpcType.ChooseSeed, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Handle(ReplantedClientData sender, PacketReader packetReader)
    {
        // Read the chosen seed type from the packet
        var seedType = (SeedType)packetReader.ReadInt();
        var SeedChooserScreen = Instances.GameplayActivity.SeedChooserScreen;
        var theChosenSeed = SeedChooserScreen.GetChosenSeedFromType(seedType);

        // Use player index 1 (opposite player) when choosing seed for remote player
        SeedChooserScreen.ClickedSeedInChooserOriginal(theChosenSeed, ReplantedOnlineMod.Constants.OPPONENT_PLAYER_INDEX);
    }
}

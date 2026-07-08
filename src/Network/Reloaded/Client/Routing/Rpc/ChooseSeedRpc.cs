using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Reloaded.Client.Routing.Packet;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Networked;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Rpc;

[RegisterRpc(RpcType.ChooseSeed)]
internal sealed class ChooseSeedRpc : IRpcMessage<ChosenSeed>
{
    /// <inheritdoc/>
    public void Send(ChosenSeed theChosenSeed)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteEnum(theChosenSeed.mSeedType);
        NetworkManager.Packet<RpcPacket>.Singleton.Send(RpcType.ChooseSeed, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader)
    {
        // Read the chosen seed type from the packet
        var seedType = packetReader.ReadEnum<SeedType>();
        var SeedChooserScreen = Instances.GameplayActivity.SeedChooserScreen;
        var theChosenSeed = SeedChooserScreen.GetChosenSeedFromType(seedType);

        // Use player index 1 (opposite player) when choosing seed for remote player
        SeedChooserScreen.ClickedSeedInChooserOriginal(theChosenSeed, ReplantedOnlineMod.Constants.Reloaded.OPPONENT_PLAYER_INDEX);
    }
}

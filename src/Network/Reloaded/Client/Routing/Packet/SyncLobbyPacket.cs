using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Structs.Network;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacket(PacketType.SyncLobby)]
internal sealed class SyncLobbyPacket : IPacketMessage<ID>
{
    /// <inheritdoc/>
    public void Send(ID targetId)
    {
        PacketWriter packetWriter = NetworkManager.StartPacket(PacketType.SyncLobby);
        ReloadedLobby.LobbyData!.VersusModeConfig.Serialize(packetWriter);
        NetworkManager.EndPacketAndSendTo(targetId, packetWriter, PacketChannel.Main, true);
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        if (!sender.AmHost)
            return;

        ReloadedLobby.LobbyData!.VersusModeConfig.Deserialize(packetReader);
        ReloadedClientData.LocalClient?.ReceivedConfig.Value = true;
    }
}

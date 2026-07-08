using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Serialization;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacket(PacketType.ResetLobby)]
internal sealed class ResetLobbyPacket : IPacketMessage
{
    /// <inheritdoc/>
    public void Send()
    {
        NetworkManager.SendPacket(null, PacketType.ResetLobby, PacketChannel.Main, false);
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        if (sender.AmHost)
        {
            ReloadedLobby.ResetLobby();
        }
    }
}

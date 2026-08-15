using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Reloaded.Panel;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.MelonLoader;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacket(PacketType.RemoveClient)]
internal sealed class RemoveClientPacket : IPacketMessage<ID, BanReason>
{
    /// <inheritdoc/>
    public void Send(ID clientId, BanReason banReason)
    {
        var packetWriter = NetworkManager.StartPacket(PacketType.RemoveClient);
        packetWriter.WriteEnum(banReason);
        NetworkManager.EndPacketAndSendTo(clientId, packetWriter, PacketChannel.Main, true);
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        if (local)
        {
            return;
        }

        if (sender.AmHost && !ReloadedLobby.AmLobbyHost())
        {
            var reason = packetReader.ReadEnum<BanReason>();
            ReloadedLobby.LeaveLobby(() =>
            {
                CustomPopupPanel.Show("Disconnected", "You have been disconnected by the Host!");
            });
            ReplantedOnlineMod.Logger.Msg(typeof(NetworkManager), "P2P closed by host");
        }
    }
}

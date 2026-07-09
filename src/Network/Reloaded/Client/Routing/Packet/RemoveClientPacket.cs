using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Reloaded.Panel;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.MelonLoader;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacket(PacketType.RemoveClient)]
internal sealed class RemoveClientPacket : IPacketMessage<ID, BanReason>
{
    /// <inheritdoc/>
    public void Send(ID clientId, BanReason banReason)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteEnum(banReason);
        NetworkManager.SendPacketTo(clientId, packetWriter, PacketType.RemoveClient, PacketChannel.Main, true);
        packetWriter.Recycle();
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

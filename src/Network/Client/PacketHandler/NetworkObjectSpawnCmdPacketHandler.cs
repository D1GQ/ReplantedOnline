using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Packet.Messages;
using ReplantedOnline.Network.Routing;
using ReplantedOnline.Utilities.MelonLoader;

namespace ReplantedOnline.Network.Client.PacketHandler;

[RegisterPacketHandler(PacketHandlerType.NetworkObjectSpawnCmd)]
internal sealed class NetworkObjectSpawnCmdPacketHandler : IPacketHandler
{
    /// <inheritdoc/>
    public void Handle(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        if (!ReloadedLobby.AmLobbyHost()) return;

        var packet = PacketReader.Get(packetReader.GetByteBuffer());
        var message = Message<NetworkObjectSpawnMessage>.Instance.Deserialize(packet);

        if (Validate(sender, message))
        {
            NetworkDispatcher.SendPacket(PacketWriter.Get(packetReader.GetByteBuffer()), true, PacketHandlerType.NetworkObjectSpawn, PacketChannel.Main, sender.ClientId);
            ReplantedOnlineMod.Logger.Msg(typeof(NetworkObjectSpawnCmdPacketHandler), $"{sender.Name}: Is requesting to spawn network object {message.NetworkId}, Prefab: {message.PrefabId}");
        }
        else
        {
            NetworkDispatcher.RejectNetworkObject(message.NetworkId, sender.ClientId);
        }
    }

    private static bool Validate(ReloadedClientData sender, NetworkObjectSpawnMessage message)
    {
        if (sender.GetClientIndex() != message.NetworkId.ClientIndex)
        {
            return false;
        }

        return true;
    }
}

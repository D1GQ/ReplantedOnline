using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Network.Reloaded.Serialization.Messages;
using ReplantedOnline.Utilities.MelonLoader;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacketHandler(PacketType.NetworkObjectReject)]
internal sealed class NetworkObjectRejectPacketHandler : IPacketHandler
{
    /// <inheritdoc/>
    public void Handle(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        if (!sender.AmHost) return;

        var packet = PacketReader.Get(packetReader.GetByteBuffer());
        var message = Message<NetworkObjectRejectMessage>.Instance.Deserialize(packet);

        try
        {
            if (ReloadedLobby.LobbyData!.NetworkObjectsSpawned.TryGetValue(message.NetworkId, out var networkObj))
            {
                networkObj.OnRejected();
                UnityEngine.Object.Destroy(networkObj);
                ReloadedLobby.LobbyData.OnNetworkObjectDespawn(networkObj);
                ReplantedOnlineMod.Logger.Msg(typeof(NetworkObjectRejectPacketHandler), $"Network Object({message.NetworkId}) rejected by host!");
            }
        }
        finally
        {
            packet.Recycle();
        }
    }
}

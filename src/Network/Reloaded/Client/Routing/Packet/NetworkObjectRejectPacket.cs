using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Network.Reloaded.Serialization.Messages;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.MelonLoader;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacket(PacketType.NetworkObjectReject)]
internal sealed class NetworkObjectRejectPacket : IPacketMessage<NetworkIdentifier, ID>
{
    /// <inheritdoc/>
    public void Send(NetworkIdentifier networkId, ID owner)
    {
        if (owner.GetNetClient()!.AmLocal) return;

        PacketWriter packetWriter = PacketWriter.Get();
        Message<NetworkObjectRejectMessage>.Singleton.Serialize(packetWriter, networkId);

        ReplantedOnlineMod.Logger.Msg(typeof(NetworkManager), $"Sent Reject Network Object with ID: {networkId}, Owner: {owner}");
        NetworkManager.SendPacketTo(owner, packetWriter, PacketType.NetworkObjectReject, PacketChannel.Main);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        if (!sender.AmHost) return;

        var packet = PacketReader.Get(packetReader.GetByteBuffer());
        var message = Message<NetworkObjectRejectMessage>.Singleton.Deserialize(packet);

        try
        {
            if (ReloadedLobby.LobbyData!.NetworkObjectsSpawned.TryGetValue(message.NetworkId, out var networkObj))
            {
                networkObj.OnRejected();
                UnityEngine.Object.Destroy(networkObj);
                ReloadedLobby.LobbyData.OnNetworkObjectDespawn(networkObj);
                ReplantedOnlineMod.Logger.Msg(typeof(NetworkObjectRejectPacket), $"Network Object({message.NetworkId}) rejected by host!");
            }
        }
        finally
        {
            packet.Recycle();
        }
    }
}

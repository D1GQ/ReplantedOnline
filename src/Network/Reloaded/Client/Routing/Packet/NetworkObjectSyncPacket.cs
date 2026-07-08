using MelonLoader;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Network.Reloaded.Serialization.Messages;
using ReplantedOnline.Utilities.MelonLoader;
using System.Collections;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacketHandler(PacketType.NetworkObjectSync)]
internal sealed class NetworkObjectSyncPacket : IPacketMessage
{
    /// <inheritdoc/>
    public void Send()
    {
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        MelonCoroutines.Start(CoWaitForNetworkObjectSync(sender, packetReader));
    }

    private static IEnumerator CoWaitForNetworkObjectSync(ReloadedClientData sender, PacketReader packetReader)
    {
        var packet = PacketReader.Get(packetReader.GetByteBuffer());
        var message = Message<NetworkObjectSyncMessage>.Singleton.Deserialize(packet);

        try
        {
            while (ReloadedLobby.LobbyData != null)
            {
                if (ReloadedLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(message.NetworkId, out var networkObj))
                {
                    if (networkObj.OwnerId != sender.ClientId)
                    {
                        ReplantedOnlineMod.Logger.Warning(typeof(NetworkObjectSyncPacket), $"Sync rejected: {sender.Name} is not owner of NetworkObject {message.NetworkId}");
                        break;
                    }

                    networkObj.SyncedBits.DirtyBits = message.DirtyBits;
                    networkObj.Deserialize(packet, message.Init);
                    ReplantedOnlineMod.Logger.Msg(typeof(NetworkObjectSyncPacket), $"Synced NetworkObject from {sender.Name}: {message.NetworkId}");
                    break;
                }

                yield return null;
            }
        }
        finally
        {
            packet.Recycle();
        }
    }
}

using MelonLoader;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Packet.Messages;
using System.Collections;

namespace ReplantedOnline.Network.Client.PacketHandler;

[RegisterPacketHandler]
internal sealed class NetworkObjectSyncPacketHandler : IPacketHandler
{
    /// <inheritdoc/>
    public PacketHandlerType Type => PacketHandlerType.NetworkObjectSync;

    /// <inheritdoc/>
    public void Handle(ReplantedClientData sender, PacketReader packetReader)
    {
        MelonCoroutines.Start(CoWaitForNetworkObjectSync(sender, packetReader));
    }

    private static IEnumerator CoWaitForNetworkObjectSync(ReplantedClientData sender, PacketReader packetReader)
    {
        var packet = PacketReader.Get(packetReader.GetByteBuffer());
        var networkSyncMessage = Message<NetworkSyncMessage>.Instance.Deserialize(packet);

        try
        {
            while (ReplantedLobby.LobbyData != null)
            {
                if (ReplantedLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(networkSyncMessage.NetworkId, out var networkObj))
                {
                    if (networkObj.OwnerId != sender.ClientId)
                    {
                        ReplantedOnlineMod.Logger.Warning($"[NetworkDispatcher] Sync rejected: {sender.Name} is not owner of NetworkObject {networkSyncMessage.NetworkId}");
                        break;
                    }

                    networkObj.SyncedBits.SyncedDirtyBits = networkSyncMessage.DirtyBits;
                    networkObj.Deserialize(packet, networkSyncMessage.Init);
                    ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Synced NetworkObject from {sender.Name}: {networkSyncMessage.NetworkId}");
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

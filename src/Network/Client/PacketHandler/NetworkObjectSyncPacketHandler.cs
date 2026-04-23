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
        var message = Message<NetworkObjectSyncMessage>.Instance.Deserialize(packet);

        try
        {
            while (ReplantedLobby.LobbyData != null)
            {
                if (ReplantedLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(message.NetworkId, out var networkObj))
                {
                    if (networkObj.OwnerId != sender.ClientId)
                    {
                        ReplantedOnlineMod.Logger.Warning($"[NetworkDispatcher] Sync rejected: {sender.Name} is not owner of NetworkObject {message.NetworkId}");
                        break;
                    }

                    networkObj.SyncedBits.SyncedDirtyBits = message.DirtyBits;
                    networkObj.Deserialize(packet, message.Init);
                    ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Synced NetworkObject from {sender.Name}: {message.NetworkId}");
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

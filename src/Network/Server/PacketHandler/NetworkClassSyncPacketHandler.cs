using MelonLoader;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Server.Packet;
using System.Collections;

namespace ReplantedOnline.Network.Server.PacketHandler;

[RegisterPacketHandler]
internal sealed class NetworkClassSyncPacketHandler : IPacketHandler
{
    /// <inheritdoc/>
    public PacketTag Tag => PacketTag.NetworkClassSync;

    /// <inheritdoc/>
    public void Handle(NetClient sender, PacketReader packetReader)
    {
        MelonCoroutines.Start(CoWaitForNetworkClassSync(sender, packetReader));
    }

    private static IEnumerator CoWaitForNetworkClassSync(NetClient sender, PacketReader packetReader)
    {
        var packet = PacketReader.Get(packetReader);
        var networkSyncPacket = NetworkSyncPacket.DeserializePacket(packet);

        try
        {
            while (NetLobby.LobbyData != null)
            {
                if (NetLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(networkSyncPacket.NetworkId, out var networkObj))
                {
                    if (networkObj.OwnerId != sender.ClientId)
                    {
                        ReplantedOnlineMod.Logger.Warning($"[NetworkDispatcher] Sync rejected: {sender.Name} is not owner of NetworkClass {networkSyncPacket.NetworkId}");
                        break;
                    }

                    networkObj.SyncedBits.SyncedDirtyBits = networkSyncPacket.DirtyBits;
                    networkObj.Deserialize(packet, networkSyncPacket.Init);
                    ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Synced NetworkClass from {sender.Name}: {networkSyncPacket.NetworkId}");
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

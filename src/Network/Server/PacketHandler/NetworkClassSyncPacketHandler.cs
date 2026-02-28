using MelonLoader;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Network.Server.Packet;
using ReplantedOnline.Network.Steam;
using System.Collections;

namespace ReplantedOnline.Network.Server.PacketHandler;

[RegisterPacketHandler]
internal sealed class NetworkClassSyncPacketHandler : BasePacketHandler
{
    /// <inheritdoc/>
    internal sealed override PacketTag Tag => PacketTag.NetworkClassSync;

    /// <inheritdoc/>
    internal sealed override void Handle(SteamNetClient sender, PacketReader packetReader)
    {
        MelonCoroutines.Start(CoWaitForNetworkClassSync(sender, packetReader));
    }

    private static IEnumerator CoWaitForNetworkClassSync(SteamNetClient sender, PacketReader packetReader)
    {
        var packet = PacketReader.Get(packetReader);
        var networkSyncPacket = NetworkSyncPacket.DeserializePacket(packet);

        try
        {
            while (NetLobby.LobbyData != null)
            {
                if (NetLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(networkSyncPacket.NetworkId, out var networkObj))
                {
                    if (networkObj.OwnerId != sender.SteamId)
                    {
                        MelonLogger.Warning($"[NetworkDispatcher] Sync rejected: {sender.Name} is not owner of NetworkClass {networkSyncPacket.NetworkId}");
                        break;
                    }

                    networkObj.SyncedBits.SyncedDirtyBits = networkSyncPacket.DirtyBits;
                    networkObj.Deserialize(packet, networkSyncPacket.Init);
                    MelonLogger.Msg($"[NetworkDispatcher] Synced NetworkClass from {sender.Name}: {networkSyncPacket.NetworkId}");
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

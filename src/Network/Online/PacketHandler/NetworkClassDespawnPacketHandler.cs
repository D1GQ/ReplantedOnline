using MelonLoader;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Network.Packet;

namespace ReplantedOnline.Network.Online.PacketHandler;

[RegisterPacketHandler]
internal sealed class NetworkClassDespawnPacketHandler : BasePacketHandler
{
    /// <inheritdoc/>
    internal sealed override PacketTag Tag => PacketTag.NetworkClassDespawn;

    /// <inheritdoc/>
    internal sealed override void Handle(SteamNetClient sender, PacketReader packetReader)
    {
        var networkDespawnPacket = NetworkDespawnPacket.DeserializePacket(packetReader);

        if (NetLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(networkDespawnPacket.NetworkId, out var networkObj))
        {
            if (networkObj.OwnerId == sender.SteamId)
            {
                if (!networkObj.AmChild)
                {
                    NetLobby.LobbyData.OnNetworkObjectDespawn(networkObj);
                    UnityEngine.Object.Destroy(networkObj.gameObject);
                    MelonLogger.Msg($"[NetworkDispatcher] Despawned NetworkClass from {sender.Name}: {networkDespawnPacket.NetworkId}");
                }
                else
                {
                    MelonLogger.Error($"[NetworkDispatcher] {sender.Name} Client requested to despawn child network object {networkDespawnPacket.NetworkId}, only the parent can be despawned!");
                }
            }
        }
        else
        {
            MelonLogger.Warning($"[NetworkDispatcher] Failed to despawn NetworkClass: ID {networkDespawnPacket.NetworkId} not found");
        }
    }
}

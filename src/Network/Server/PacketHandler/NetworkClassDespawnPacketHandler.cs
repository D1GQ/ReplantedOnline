using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Server.Packet;

namespace ReplantedOnline.Network.Server.PacketHandler;

[RegisterPacketHandler]
internal sealed class NetworkClassDespawnPacketHandler : BasePacketHandler
{
    /// <inheritdoc/>
    internal sealed override PacketTag Tag => PacketTag.NetworkClassDespawn;

    /// <inheritdoc/>
    internal sealed override void Handle(NetClient sender, PacketReader packetReader)
    {
        var networkDespawnPacket = NetworkDespawnPacket.DeserializePacket(packetReader);

        if (NetLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(networkDespawnPacket.NetworkId, out var networkObj))
        {
            if (networkObj.OwnerId == sender.ClientId)
            {
                if (!networkObj.AmChild)
                {
                    NetLobby.LobbyData.OnNetworkObjectDespawn(networkObj);
                    UnityEngine.Object.Destroy(networkObj.gameObject);
                    ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Despawned NetworkClass from {sender.Name}: {networkDespawnPacket.NetworkId}");
                }
                else
                {
                    ReplantedOnlineMod.Logger.Error($"[NetworkDispatcher] {sender.Name} Client requested to despawn child network object {networkDespawnPacket.NetworkId}, only the parent can be despawned!");
                }
            }
        }
        else
        {
            ReplantedOnlineMod.Logger.Warning($"[NetworkDispatcher] Failed to despawn NetworkClass: ID {networkDespawnPacket.NetworkId} not found");
        }
    }
}

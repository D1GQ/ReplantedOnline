using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Packet.Messages;

namespace ReplantedOnline.Network.Client.PacketHandler;

[RegisterPacketHandler]
internal sealed class NetworkObjectDespawnPacketHandler : IPacketHandler
{
    /// <inheritdoc/>
    public PacketHandlerType Type => PacketHandlerType.NetworkObjectDespawn;

    /// <inheritdoc/>
    public void Handle(ReplantedClientData sender, PacketReader packetReader)
    {
        var networkDespawnMessage = Message<NetworkDespawnMessage>.Instance.Deserialize(packetReader);

        if (ReplantedLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(networkDespawnMessage.NetworkId, out var networkObj))
        {
            if (networkObj.OwnerId == sender.ClientId)
            {
                if (!networkObj.AmChild)
                {
                    ReplantedLobby.LobbyData.OnNetworkObjectDespawn(networkObj);
                    UnityEngine.Object.Destroy(networkObj.gameObject);
                    ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Despawned NetworkObject from {sender.Name}: {networkDespawnMessage.NetworkId}");
                }
                else
                {
                    ReplantedOnlineMod.Logger.Error($"[NetworkDispatcher] {sender.Name} Client requested to despawn child network object {networkDespawnMessage.NetworkId}, only the parent can be despawned!");
                }
            }
        }
        else
        {
            ReplantedOnlineMod.Logger.Warning($"[NetworkDispatcher] Failed to despawn NetworkObject: ID {networkDespawnMessage.NetworkId} not found");
        }
    }
}

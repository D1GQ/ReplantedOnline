using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client.Object;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Packet.Messages;

namespace ReplantedOnline.Network.Client.PacketHandler;

[RegisterPacketHandler]
internal sealed class NetworkObjectSpawnPacketHandler : IPacketHandler
{
    /// <inheritdoc/>
    public PacketHandlerType Type => PacketHandlerType.NetworkObjectSpawn;

    /// <inheritdoc/>
    public void Handle(ReplantedClientData sender, PacketReader packetReader)
    {
        var spawnMessage = Message<NetworkSpawnMessage>.Instance.Deserialize(packetReader);

        if (spawnMessage.PrefabId == NetworkObject.NO_PREFAB_ID)
        {
            ReplantedOnlineMod.Logger.Error("Serialized network object had a unset prefab id!");
        }
        else
        {
            if (NetworkObject.NetworkPrefabs.TryGetValue(spawnMessage.PrefabId, out var prefab))
            {
                var networkObj = prefab.Clone<NetworkObject>();
                networkObj.OwnerId = spawnMessage.OwnerId;
                networkObj.NetworkId = spawnMessage.NetworkId;
                networkObj.transform.SetParent(NetworkObject.NetworkObjectsGo.transform);
                Message<NetworkSpawnMessage>.Instance.DeserializeNetworkObject(networkObj, packetReader);
                networkObj.gameObject.SetActive(true);
                ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Spawned prefab NetworkClass from {sender.Name}: {spawnMessage.NetworkId}, Prefab: {spawnMessage.PrefabId}");
            }
            else
            {
                ReplantedOnlineMod.Logger.Error($"[NetworkDispatcher] Failed to spawn NetworkClass: Prefab ID {spawnMessage.PrefabId} not found");
            }
        }
    }
}

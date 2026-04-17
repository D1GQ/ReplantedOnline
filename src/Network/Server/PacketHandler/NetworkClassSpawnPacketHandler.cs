using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Client.Object;
using ReplantedOnline.Network.Server.Packet;

namespace ReplantedOnline.Network.Server.PacketHandler;

[RegisterPacketHandler]
internal sealed class NetworkClassSpawnPacketHandler : IPacketHandler
{
    /// <inheritdoc/>
    public PacketTag Tag => PacketTag.NetworkClassSpawn;

    /// <inheritdoc/>
    public void Handle(NetClient sender, PacketReader packetReader)
    {
        var spawnPacket = NetworkSpawnPacket.DeserializePacket(packetReader);

        if (spawnPacket.PrefabId == NetworkObject.NO_PREFAB_ID)
        {
            ReplantedOnlineMod.Logger.Error("Serialized network object had a unset prefab id!");
        }
        else
        {
            if (NetworkObject.NetworkPrefabs.TryGetValue(spawnPacket.PrefabId, out var prefab))
            {
                var networkObj = prefab.Clone<NetworkObject>();
                networkObj.OwnerId = spawnPacket.OwnerId;
                networkObj.NetworkId = spawnPacket.NetworkId;
                networkObj.transform.SetParent(NetworkObject.NetworkObjectsGo.transform);
                NetworkSpawnPacket.DeserializeNetworkObject(networkObj, packetReader);
                networkObj.gameObject.SetActive(true);
                ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Spawned prefab NetworkClass from {sender.Name}: {spawnPacket.NetworkId}, Prefab: {spawnPacket.PrefabId}");
            }
            else
            {
                ReplantedOnlineMod.Logger.Error($"[NetworkDispatcher] Failed to spawn NetworkClass: Prefab ID {spawnPacket.PrefabId} not found");
            }
        }
    }
}

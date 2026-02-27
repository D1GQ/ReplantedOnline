using MelonLoader;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Network.Object;
using ReplantedOnline.Network.Packet;

namespace ReplantedOnline.Network.Online.PacketHandler;

[RegisterPacketHandler]
internal sealed class NetworkClassSpawnPacketHandler : BasePacketHandler
{
    /// <inheritdoc/>
    internal sealed override PacketTag Tag => PacketTag.NetworkClassSpawn;

    /// <inheritdoc/>
    internal sealed override void Streamline(SteamNetClient sender, PacketReader packetReader)
    {
        var spawnPacket = NetworkSpawnPacket.DeserializePacket(packetReader);

        if (spawnPacket.PrefabId == NetworkObject.NO_PREFAB_ID)
        {
            MelonLogger.Error("Serialized network object had a unset prefab id!");
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
                NetLobby.LobbyData.OnNetworkObjectSpawn(networkObj);
                networkObj.gameObject.SetActive(true);
                networkObj.name = $"{networkObj.GetType().Name}({networkObj.NetworkId})";
                MelonLogger.Msg($"[NetworkDispatcher] Spawned prefab NetworkClass from {sender.Name}: {spawnPacket.NetworkId}, Prefab: {spawnPacket.PrefabId}");
            }
            else
            {
                MelonLogger.Error($"[NetworkDispatcher] Failed to spawn NetworkClass: Prefab ID {spawnPacket.PrefabId} not found");
            }
        }
    }
}

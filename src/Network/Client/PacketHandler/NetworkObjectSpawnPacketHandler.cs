using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules;
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
        var message = Message<NetworkObjectSpawnMessage>.Instance.Deserialize(packetReader);

        if (message.PrefabId == NetworkObject.NO_PREFAB_ID)
        {
            ReplantedOnlineMod.Logger.Error("Serialized network object had a unset prefab id!");
        }
        else
        {
            if (NetworkObject.NetworkPrefabs.TryGetValue(message.PrefabId, out var prefab))
            {
                var networkObj = prefab.Clone<NetworkObject>();
                networkObj.OwnerId = message.OwnerId;
                networkObj.NetworkId = message.NetworkId;
                networkObj.transform.SetParent(GlobalGameObjects.NetworkObjectsGo.transform);
                Message<NetworkObjectSpawnMessage>.Instance.DeserializeNetworkObject(networkObj, packetReader);
                networkObj.gameObject.SetActive(true);
                ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Spawned prefab NetworkObject from {sender.Name}: {message.NetworkId}, Prefab: {message.PrefabId}");
            }
            else
            {
                ReplantedOnlineMod.Logger.Error($"[NetworkDispatcher] Failed to spawn NetworkObject: Prefab ID {message.PrefabId} not found");
            }
        }
    }
}

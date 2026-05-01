using MelonLoader;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Client.Object;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Packet.Messages;
using ReplantedOnline.Utilities;
using System.Collections;

namespace ReplantedOnline.Network.Client.PacketHandler;

[RegisterPacketHandler]
internal sealed class NetworkObjectSpawnPacketHandler : IPacketHandler
{
    /// <inheritdoc/>
    public PacketHandlerType Type => PacketHandlerType.NetworkObjectSpawn;

    /// <inheritdoc/>
    public void Handle(ReplantedClientData sender, PacketReader packetReader, bool local)
    {
        MelonCoroutines.Start(CoWaitForNetworkSpawn(sender, packetReader));
    }

    private static IEnumerator CoWaitForNetworkSpawn(ReplantedClientData sender, PacketReader packetReader)
    {
        var packet = PacketReader.Get(packetReader.GetByteBuffer());
        var message = Message<NetworkObjectSpawnMessage>.Instance.Deserialize(packet);

        try
        {
            while (ReplantedLobby.LobbyData != null)
            {
                if (!ReplantedLobby.LobbyData.ReadyForNetworkObjects)
                {
                    yield return null;
                    continue;
                }

                if (message.PrefabId == NetworkObject.NO_PREFAB_ID)
                {
                    ReplantedOnlineMod.Logger.Error(typeof(NetworkObjectSpawnPacketHandler), "Serialized network object had a unset prefab id!");
                }
                else
                {
                    if (NetworkObject.NetworkPrefabs.TryGetValue(message.PrefabId, out var prefab))
                    {
                        var networkObj = prefab.Clone<NetworkObject>();
                        networkObj.OwnerId = message.OwnerId;
                        networkObj.NetworkId = message.NetworkId;
                        networkObj.transform.SetParent(GlobalGameObjects.NetworkObjectsGo.transform);
                        Message<NetworkObjectSpawnMessage>.Instance.DeserializeNetworkObject(networkObj, packet);
                        networkObj.gameObject.SetActive(true);
                        networkObj.OnEnabled();
                        ReplantedOnlineMod.Logger.Msg(typeof(NetworkObjectSpawnPacketHandler), $"Spawned prefab NetworkObject from {sender.Name}: {message.NetworkId}, Prefab: {message.PrefabId}");
                    }
                    else
                    {
                        ReplantedOnlineMod.Logger.Error(typeof(NetworkObjectSpawnPacketHandler), $"Failed to spawn NetworkObject: Prefab ID {message.PrefabId} not found");
                    }
                }

                break;
            }
        }
        finally
        {
            packet.Recycle();
        }
    }
}

using MelonLoader;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Unity;
using ReplantedOnline.Network.Reloaded.Client.Object;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Network.Reloaded.Serialization.Messages;
using ReplantedOnline.Utilities.MelonLoader;
using System.Collections;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacketHandler(PacketType.NetworkObjectSpawn)]
internal sealed class NetworkObjectSpawnPacketHandler : IPacketHandler
{
    /// <inheritdoc/>
    public void Handle(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        if (!sender.AmHost) return;

        MelonCoroutines.Start(CoWaitForNetworkSpawn(sender, packetReader));
    }

    private static IEnumerator CoWaitForNetworkSpawn(ReloadedClientData sender, PacketReader packetReader)
    {
        var packet = PacketReader.Get(packetReader.GetByteBuffer());
        var message = Message<NetworkObjectSpawnMessage>.Instance.Deserialize(packet);

        try
        {
            while (ReloadedLobby.LobbyData != null)
            {
                if (!ReloadedLobby.LobbyData.ReadyForNetworkObjects)
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

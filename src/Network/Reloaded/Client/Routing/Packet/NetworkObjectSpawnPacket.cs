using MelonLoader;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Unity;
using ReplantedOnline.Network.Reloaded.Client.Object;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Network.Reloaded.Serialization.Messages;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.MelonLoader;
using System.Collections;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacketHandler(PacketType.NetworkObjectSpawn)]
internal sealed class NetworkObjectSpawnPacket : IPacketMessage<NetworkObject, ID>
{
    /// <inheritdoc/>
    public void Send(NetworkObject networkObj, ID owner)
    {
        networkObj.OwnerId = owner;
        networkObj.NetworkId = ReloadedLobby.LobbyData!.NetworkIdPool.Allocate();

        if (!ReloadedLobby.AmLobbyHost())
        {
            NetworkManager.Packet<NetworkObjectSpawnCmdPacket>.Singleton.Send(networkObj);
            return;
        }

        PacketWriter packetWriter = PacketWriter.Get();
        Message<NetworkObjectSpawnMessage>.Singleton.Serialize(packetWriter, networkObj);

        ReplantedOnlineMod.Logger.Msg(typeof(NetworkManager), $"Sent Spawn Network Object with ID: {networkObj.NetworkId}, Owner: {owner}");
        NetworkManager.SendPacket(packetWriter, PacketType.NetworkObjectSpawn, PacketChannel.Main, false);
        packetWriter.Recycle();
    }

    /// <summary>
    /// Spawns all Active network objects to a new client
    /// </summary>
    /// <param name="targetId">The ID of the target client to receive the packet.</param>
    internal void SendNetworkObjectsTo(ID targetId)
    {
        if (!ReloadedLobby.AmLobbyHost())
            return;

        if (ReloadedLobby.LobbyData!.NetworkObjectsSpawned.Count > 0)
        {
            foreach (var networkObj in ReloadedLobby.LobbyData.NetworkObjectsSpawned.Values)
            {
                if (networkObj.IsOnNetwork && !networkObj.AmChild)
                {
                    PacketWriter packetWriter = PacketWriter.Get();
                    Message<NetworkObjectSpawnMessage>.Singleton.Serialize(packetWriter, networkObj);

                    ReplantedOnlineMod.Logger.Msg(typeof(NetworkManager), $"Sent Network Objects to {targetId}");
                    NetworkManager.SendPacketTo(targetId, packetWriter, PacketType.NetworkObjectSpawn, PacketChannel.Buffered);
                    packetWriter.Recycle();
                }
            }
        }
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        if (!sender.AmHost) return;

        MelonCoroutines.Start(CoWaitForNetworkSpawn(sender, packetReader));
    }

    private static IEnumerator CoWaitForNetworkSpawn(ReloadedClientData sender, PacketReader packetReader)
    {
        var packet = PacketReader.Get(packetReader.GetByteBuffer());
        var message = Message<NetworkObjectSpawnMessage>.Singleton.Deserialize(packet);

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
                    ReplantedOnlineMod.Logger.Error(typeof(NetworkObjectSpawnPacket), "Serialized network object had a unset prefab id!");
                }
                else
                {
                    if (NetworkObject.NetworkPrefabs.TryGetValue(message.PrefabId, out var prefab))
                    {
                        var networkObj = prefab.Clone<NetworkObject>();
                        networkObj.OwnerId = message.OwnerId;
                        networkObj.NetworkId = message.NetworkId;
                        networkObj.transform.SetParent(GlobalGameObjects.NetworkObjectsGo.transform);
                        Message<NetworkObjectSpawnMessage>.Singleton.DeserializeNetworkObject(networkObj, packet);
                        networkObj.gameObject.SetActive(true);
                        networkObj.OnEnabled();
                        ReplantedOnlineMod.Logger.Msg(typeof(NetworkObjectSpawnPacket), $"Spawned prefab NetworkObject from {sender.Name}: {message.NetworkId}, Prefab: {message.PrefabId}");
                    }
                    else
                    {
                        ReplantedOnlineMod.Logger.Error(typeof(NetworkObjectSpawnPacket), $"Failed to spawn NetworkObject: Prefab ID {message.PrefabId} not found");
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

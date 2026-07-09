using MelonLoader;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Client.Object;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Network.Reloaded.Serialization.Messages;
using ReplantedOnline.Utilities.MelonLoader;
using System.Collections;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacket(PacketType.NetworkObjectDespawn)]
internal sealed class NetworkObjectDespawnPacket : IPacketMessage<NetworkObject, bool>
{
    /// <inheritdoc/>
    public void Send(NetworkObject networkObj, bool waitToBeReady)
    {
        PacketWriter packetWriter = PacketWriter.Get();
        Message<NetworkObjectDespawnMessage>.Singleton.Serialize(packetWriter, networkObj, waitToBeReady);

        ReplantedOnlineMod.Logger.Msg(typeof(NetworkManager), $"Sent Despawn Network Object with ID: {networkObj.NetworkId}");
        NetworkManager.SendPacket(packetWriter, PacketType.NetworkObjectDespawn, PacketChannel.Main, true, false);
        packetWriter.Recycle();

        ReloadedLobby.LobbyData!.OnNetworkObjectDespawn(networkObj);
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        MelonCoroutines.Start(CoWaitForNetworkDespawn(sender, packetReader));
    }

    private static IEnumerator CoWaitForNetworkDespawn(ReloadedClientData sender, PacketReader packetReader)
    {
        var packet = PacketReader.Get(packetReader.GetByteBuffer());
        var message = Message<NetworkObjectDespawnMessage>.Singleton.Deserialize(packet);

        try
        {
            while (ReloadedLobby.LobbyData != null)
            {
                if (ReloadedLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(message.NetworkId, out var networkObj))
                {
                    if (networkObj.OwnerId == sender.ClientId || sender.AmHost)
                    {
                        if (!networkObj.AmChild)
                        {
                            while (message.WaitToBeReady && !networkObj.IsReadyToDespawn)
                            {
                                yield return null;
                            }

                            ReloadedLobby.LobbyData.OnNetworkObjectDespawn(networkObj);
                            UnityEngine.Object.Destroy(networkObj.gameObject);
                            ReplantedOnlineMod.Logger.Msg(typeof(NetworkObjectDespawnPacket), $"Despawned NetworkObject from {sender.Name}: {message.NetworkId}");
                        }
                        else
                        {
                            ReplantedOnlineMod.Logger.Error(typeof(NetworkObjectDespawnPacket), $"{sender.Name} Client requested to despawn child network object {message.NetworkId}, only the parent can be despawned!");
                        }
                    }
                    break;
                }

                yield return null;
            }
        }
        finally
        {
            packet.Recycle();
        }
    }
}

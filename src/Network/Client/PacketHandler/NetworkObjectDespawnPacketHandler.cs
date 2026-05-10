using MelonLoader;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Packet.Messages;
using ReplantedOnline.Utilities.MelonLoader;
using System.Collections;

namespace ReplantedOnline.Network.Client.PacketHandler;

[RegisterPacketHandler(PacketHandlerType.NetworkObjectDespawn)]
internal sealed class NetworkObjectDespawnPacketHandler : IPacketHandler
{
    /// <inheritdoc/>
    public void Handle(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        MelonCoroutines.Start(CoWaitForNetworkDespawn(sender, packetReader));
    }

    private static IEnumerator CoWaitForNetworkDespawn(ReloadedClientData sender, PacketReader packetReader)
    {
        var packet = PacketReader.Get(packetReader.GetByteBuffer());
        var message = Message<NetworkObjectDespawnMessage>.Instance.Deserialize(packet);

        try
        {
            while (ReloadedLobby.LobbyData != null)
            {
                if (ReloadedLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(message.NetworkId, out var networkObj))
                {
                    if (networkObj.OwnerId == sender.ClientId)
                    {
                        if (!networkObj.AmChild)
                        {
                            while (message.WaitToBeReady && !networkObj.IsReadyToDespawn)
                            {
                                yield return null;
                            }

                            ReloadedLobby.LobbyData.OnNetworkObjectDespawn(networkObj);
                            UnityEngine.Object.Destroy(networkObj.gameObject);
                            ReplantedOnlineMod.Logger.Msg(typeof(NetworkObjectDespawnPacketHandler), $"Despawned NetworkObject from {sender.Name}: {message.NetworkId}");
                        }
                        else
                        {
                            ReplantedOnlineMod.Logger.Error(typeof(NetworkObjectDespawnPacketHandler), $"{sender.Name} Client requested to despawn child network object {message.NetworkId}, only the parent can be despawned!");
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

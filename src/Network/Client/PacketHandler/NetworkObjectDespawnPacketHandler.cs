using MelonLoader;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Packet.Messages;
using System.Collections;

namespace ReplantedOnline.Network.Client.PacketHandler;

[RegisterPacketHandler]
internal sealed class NetworkObjectDespawnPacketHandler : IPacketHandler
{
    /// <inheritdoc/>
    public PacketHandlerType Type => PacketHandlerType.NetworkObjectDespawn;

    /// <inheritdoc/>
    public void Handle(ReplantedClientData sender, PacketReader packetReader, bool local)
    {
        MelonCoroutines.Start(CoWaitForNetworkDespawn(sender, packetReader));
    }

    private static IEnumerator CoWaitForNetworkDespawn(ReplantedClientData sender, PacketReader packetReader)
    {
        var packet = PacketReader.Get(packetReader.GetByteBuffer());
        var message = Message<NetworkObjectDespawnMessage>.Instance.Deserialize(packet);

        try
        {
            while (ReplantedLobby.LobbyData != null)
            {
                if (ReplantedLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(message.NetworkId, out var networkObj))
                {
                    if (networkObj.OwnerId == sender.ClientId)
                    {
                        if (!networkObj.AmChild)
                        {
                            ReplantedLobby.LobbyData.OnNetworkObjectDespawn(networkObj);
                            UnityEngine.Object.Destroy(networkObj.gameObject);
                            ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Despawned NetworkObject from {sender.Name}: {message.NetworkId}");
                        }
                        else
                        {
                            ReplantedOnlineMod.Logger.Error($"[NetworkDispatcher] {sender.Name} Client requested to despawn child network object {message.NetworkId}, only the parent can be despawned!");
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

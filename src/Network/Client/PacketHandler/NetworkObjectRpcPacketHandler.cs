using MelonLoader;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Packet.Messages;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Client.PacketHandler;

[RegisterPacketHandler]
internal class NetworkObjectRpcPacketHandler : IPacketHandler
{
    /// <inheritdoc/>
    public PacketHandlerType Type => PacketHandlerType.NetworkObjectRpc;

    /// <inheritdoc/>
    public void Handle(ReplantedClientData sender, PacketReader packetReader)
    {
        MelonCoroutines.Start(CoWaitForNetworkObject(sender, packetReader));
    }

    private static IEnumerator CoWaitForNetworkObject(ReplantedClientData sender, PacketReader packetReader)
    {
        var packet = PacketReader.Get(packetReader.GetByteBuffer());
        var message = Message<NetworkObjectRpcMessage>.Instance.Deserialize(packetReader);
        float timeOut = 0f;

        try
        {
            while (ReplantedLobby.LobbyData != null && timeOut < 10f)
            {
                if (ReplantedLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(message.NetworkId, out var networkObj))
                {
                    ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Processing NetworkObject RPC from {sender.Name}: {message.RpcId} for NetworkId: {message.NetworkId}");
                    RpcHandlerAttribute.HandleNetworkObjectRpc(networkObj, sender, message.RpcId, packet);
                    break;
                }

                timeOut += Time.deltaTime;

                yield return null;
            }

        }
        finally
        {
            packet.Recycle();
        }
    }
}

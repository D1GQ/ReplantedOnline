using MelonLoader;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Packet;
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
        byte rpcId = packet.ReadByte();
        uint networkId = packet.ReadUInt();
        float timeOut = 0f;

        try
        {
            while (ReplantedLobby.LobbyData != null && timeOut < 10f)
            {
                if (ReplantedLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(networkId, out var networkObj))
                {
                    ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Processing NetworkObject RPC from {sender.Name}: {rpcId} for NetworkId: {networkId}");
                    RpcHandlerAttribute.HandleNetworkObjectRpc(networkObj, sender, rpcId, packet);
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

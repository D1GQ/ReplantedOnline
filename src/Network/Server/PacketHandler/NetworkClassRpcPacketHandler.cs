using MelonLoader;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Server.Packet;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Server.PacketHandler;

[RegisterPacketHandler]
internal class NetworkClassRpcPacketHandler : IPacketHandler
{
    /// <inheritdoc/>
    public PacketTag Tag => PacketTag.NetworkClassRpc;

    /// <inheritdoc/>
    public void Handle(NetClient sender, PacketReader packetReader)
    {
        MelonCoroutines.Start(CoWaitForNetworkClass(sender, packetReader));
    }

    private static IEnumerator CoWaitForNetworkClass(NetClient sender, PacketReader packetReader)
    {
        var packet = PacketReader.Get(packetReader.GetByteBuffer());
        byte rpcId = packet.ReadByte();
        uint networkId = packet.ReadUInt();
        float timeOut = 0f;

        try
        {
            while (NetLobby.LobbyData != null && timeOut < 10f)
            {
                if (NetLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(networkId, out var networkObj))
                {
                    ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Processing NetworkClass RPC from {sender.Name}: {rpcId} for NetworkId: {networkId}");
                    networkObj.HandleRpc(sender, rpcId, packet);
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

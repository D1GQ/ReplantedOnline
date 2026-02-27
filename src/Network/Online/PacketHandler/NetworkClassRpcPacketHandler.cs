using MelonLoader;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Network.Packet;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Online.PacketHandler;

[RegisterPacketHandler]
internal class NetworkClassRpcPacketHandler : BasePacketHandler
{
    /// <inheritdoc/>
    internal sealed override PacketTag Tag => PacketTag.NetworkClassRpc;

    /// <inheritdoc/>
    internal sealed override void Handle(SteamNetClient sender, PacketReader packetReader)
    {
        MelonCoroutines.Start(CoWaitForNetworkClass(sender, packetReader));
    }

    private static IEnumerator CoWaitForNetworkClass(SteamNetClient sender, PacketReader packetReader)
    {
        var packet = PacketReader.Get(packetReader);
        byte rpcId = packet.ReadByte();
        uint networkId = packet.ReadUInt();
        float timeOut = 0f;

        try
        {
            while (NetLobby.LobbyData != null && timeOut < 10f)
            {
                if (NetLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(networkId, out var networkObj))
                {
                    MelonLogger.Msg($"[NetworkDispatcher] Processing NetworkClass RPC from {sender.Name}: {rpcId} for NetworkId: {networkId}");
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

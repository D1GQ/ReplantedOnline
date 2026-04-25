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
internal class ObjectRpcPacketHandler : IPacketHandler
{
    /// <inheritdoc/>
    public PacketHandlerType Type => PacketHandlerType.ObjectRpc;

    /// <inheritdoc/>
    public void Handle(ReplantedClientData sender, PacketReader packetReader, bool local)
    {
        MelonCoroutines.Start(CoWaitForNetworkObject(sender, packetReader));
    }

    private static IEnumerator CoWaitForNetworkObject(ReplantedClientData sender, PacketReader packetReader)
    {
        var packet = PacketReader.Get(packetReader.GetByteBuffer());
        var message = Message<ObjectRpcMessage>.Instance.Deserialize(packet);
        float timeOut = 0f;

        try
        {
            while (ReplantedLobby.LobbyData != null && timeOut < 10f)
            {
                if (ReplantedLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(message.NetworkId, out var networkObj))
                {
                    if (!message.IsComponent)
                    {
                        ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Processing NetworkObject RPC from {sender.Name}: {message.RpcId} for NetworkId: {message.NetworkId}");
                        RpcHandlerAttribute.HandleRpcReceiver(networkObj, sender, message.RpcId, packet);
                    }
                    else
                    {
                        var component = networkObj.NetworkComponents.ElementAtOrDefault(message.ComponentIndex);
                        if (component == null)
                        {
                            ReplantedOnlineMod.Logger.Error($"[NetworkDispatcher] Error processing NetworkObjectComponent RPC from {sender.Name}: {message.RpcId} for NetworkId: {message.NetworkId} Component at Index: {message.ComponentIndex} not found!");
                            break;
                        }

                        ReplantedOnlineMod.Logger.Msg($"[NetworkDispatcher] Processing NetworkObjectComponent RPC from {sender.Name}: {message.RpcId} for NetworkId: {message.NetworkId} Component at Index: {message.ComponentIndex}");
                        RpcHandlerAttribute.HandleRpcReceiver(component, sender, message.RpcId, packet);
                    }

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

using MelonLoader;
using ReplantedOnline.Attributes.Modded;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Packet.Messages;
using ReplantedOnline.Utilities.MelonLoader;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Client.PacketHandler;

[RegisterPacketHandler(PacketHandlerType.ObjectRpc)]
internal class ObjectRpcPacketHandler : IPacketHandler
{
    /// <inheritdoc/>
    public void Handle(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        MelonCoroutines.Start(CoWaitForNetworkObject(sender, packetReader));
    }

    private static IEnumerator CoWaitForNetworkObject(ReloadedClientData sender, PacketReader packetReader)
    {
        var packet = PacketReader.Get(packetReader.GetByteBuffer());
        var message = Message<ObjectRpcMessage>.Instance.Deserialize(packet);
        float timeOut = 0f;

        try
        {
            while (ReloadedLobby.LobbyData != null && timeOut < 10f)
            {
                if (ReloadedLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(message.NetworkId, out var networkObj))
                {
                    if (!message.IsComponent)
                    {
                        ReplantedOnlineMod.Logger.Msg(typeof(ObjectRpcPacketHandler), $"Processing NetworkObject RPC from {sender.Name}: {message.RpcId} for NetworkId: {message.NetworkId}");
                        RpcHandlerAttribute.HandleRpcReceiver(networkObj, sender, message.RpcId, packet);
                    }
                    else
                    {
                        var component = networkObj.NetworkComponents.ElementAtOrDefault(message.ComponentIndex);
                        if (component == null)
                        {
                            ReplantedOnlineMod.Logger.Error(typeof(ObjectRpcPacketHandler), $"Error processing NetworkObjectComponent RPC from {sender.Name}: {message.RpcId} for NetworkId: {message.NetworkId} Component at Index: {message.ComponentIndex} not found!");
                            break;
                        }

                        ReplantedOnlineMod.Logger.Msg(typeof(ObjectRpcPacketHandler), $"Processing NetworkObjectComponent RPC from {sender.Name}: {message.RpcId} for NetworkId: {message.NetworkId} Component at Index: {message.ComponentIndex}");
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

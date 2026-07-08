using MelonLoader;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Network.Reloaded.Serialization.Messages;
using ReplantedOnline.Utilities.MelonLoader;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacketHandler(PacketType.ObjectRpc)]
internal class ObjectRpcPacket : IPacketMessage<INetworkIdentifier, byte, IPacket?, bool>
{
    /// <inheritdoc/>
    public void Send(INetworkIdentifier networkIdentifier, byte rpcId, IPacket? payload = null, bool receiveLocally = false)
    {
        PacketWriter packetWriter = PacketWriter.Get();
        Message<ObjectRpcMessage>.Singleton.Serialize(packetWriter, networkIdentifier, rpcId);
        if (payload != null)
        {
            packetWriter.WritePacketToBuffer(payload);
        }

        ReplantedOnlineMod.Logger.Msg(typeof(NetworkManager), $"Sent Object RPC: {rpcId} for NetworkId: {networkIdentifier.NetworkId}");
        NetworkManager.SendPacket(packetWriter, PacketType.ObjectRpc, PacketChannel.Rpc, receiveLocally);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        MelonCoroutines.Start(CoWaitForNetworkObject(sender, packetReader));
    }

    private static IEnumerator CoWaitForNetworkObject(ReloadedClientData sender, PacketReader packetReader)
    {
        var packet = PacketReader.Get(packetReader.GetByteBuffer());
        var message = Message<ObjectRpcMessage>.Singleton.Deserialize(packet);
        float timeOut = 0f;

        try
        {
            while (ReloadedLobby.LobbyData != null && timeOut < 10f)
            {
                if (ReloadedLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(message.NetworkId, out var networkObj))
                {
                    if (!message.IsComponent)
                    {
                        ReplantedOnlineMod.Logger.Msg(typeof(ObjectRpcPacket), $"Processing NetworkObject RPC from {sender.Name}: {message.RpcId} for NetworkId: {message.NetworkId}");
                        RpcHandlerAttribute.HandleRpcReceiver(networkObj, sender, message.RpcId, packet);
                    }
                    else
                    {
                        var component = networkObj.NetworkComponents.ElementAtOrDefault(message.ComponentIndex);
                        if (component == null)
                        {
                            ReplantedOnlineMod.Logger.Error(typeof(ObjectRpcPacket), $"Error processing NetworkObjectComponent RPC from {sender.Name}: {message.RpcId} for NetworkId: {message.NetworkId} Component at Index: {message.ComponentIndex} not found!");
                            break;
                        }

                        ReplantedOnlineMod.Logger.Msg(typeof(ObjectRpcPacket), $"Processing NetworkObjectComponent RPC from {sender.Name}: {message.RpcId} for NetworkId: {message.NetworkId} Component at Index: {message.ComponentIndex}");
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

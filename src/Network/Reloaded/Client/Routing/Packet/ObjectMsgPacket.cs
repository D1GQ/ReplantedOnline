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

[RegisterPacket(PacketType.ObjectMsg)]
internal class ObjectMsgPacket : IPacketMessage<INetworkIdentifier, uint, IPacket?, bool>
{
    /// <inheritdoc/>
    public void Send(INetworkIdentifier networkIdentifier, uint messageId, IPacket? payload = null, bool receiveLocally = false)
    {
        PacketWriter packetWriter = NetworkManager.StartPacket(PacketType.ObjectMsg);
        Message<ObjectMsgMessage>.Singleton.Serialize(packetWriter, networkIdentifier, messageId, payload);
        ReplantedOnlineMod.Logger.Msg(typeof(NetworkManager), $"Sent Object RPC: {messageId} for NetworkId: {networkIdentifier.NetworkId}");
        NetworkManager.EndPacketAndSend(packetWriter, PacketChannel.Rpc, true, receiveLocally);
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        MelonCoroutines.Start(CoWaitForNetworkObject(sender, packetReader));
    }

    private static IEnumerator CoWaitForNetworkObject(ReloadedClientData sender, PacketReader packetReader)
    {
        var packet = PacketReader.Get(packetReader.GetSubpacketBytes());
        var message = Message<ObjectMsgMessage>.Singleton.Deserialize(packet);
        float timeOut = 0f;

        try
        {
            while (ReloadedLobby.LobbyData != null && timeOut < 10f)
            {
                if (ReloadedLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(message.NetworkId, out var networkObj))
                {
                    if (!message.IsComponent)
                    {
                        ReplantedOnlineMod.Logger.Msg(typeof(ObjectMsgPacket), $"Processing NetworkObject RPC from {sender.Name}: {message.MessageId} for NetworkId: {message.NetworkId}");
                        ObjectMessageHandlerAttribute.HandleObjectMessage(networkObj, networkObj, sender, message.MessageId, packet);
                    }
                    else
                    {
                        var component = networkObj.NetworkComponents.ElementAtOrDefault(message.ComponentIndex);
                        if (component == null)
                        {
                            ReplantedOnlineMod.Logger.Error(typeof(ObjectMsgPacket), $"Error processing NetworkObjectComponent RPC from {sender.Name}: {message.MessageId} for NetworkId: {message.NetworkId} Component at Index: {message.ComponentIndex} not found!");
                            break;
                        }

                        ReplantedOnlineMod.Logger.Msg(typeof(ObjectMsgPacket), $"Processing NetworkObjectComponent RPC from {sender.Name}: {message.MessageId} for NetworkId: {message.NetworkId} Component at Index: {message.ComponentIndex}");
                        ObjectMessageHandlerAttribute.HandleObjectMessage(component, component, sender, message.MessageId, packet);
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

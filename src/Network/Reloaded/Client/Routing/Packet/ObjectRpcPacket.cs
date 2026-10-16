using MelonLoader;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Client.Object.Component;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Network.Reloaded.Serialization.Messages;
using ReplantedOnline.Structs.Network;
using System.Collections;
using UnityEngine;
using static ReplantedOnline.Network.Reloaded.Client.Routing.Packet.ObjectRpcPacket;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Packet;

[RegisterPacket(PacketType.ObjectRpc)]
internal class ObjectRpcPacket : IPacketMessage<CatchedNetworkIdentifier, uint, IPacket?>
{
    /// <inheritdoc/>
    public void Send(CatchedNetworkIdentifier networkIdentifier, uint rpcId, IPacket? payload = null)
    {
        PacketWriter packetWriter = NetworkManager.StartPacket(PacketType.ObjectRpc);
        Message<ObjectRpcMessage>.Singleton.Serialize(packetWriter, networkIdentifier, rpcId, payload);
        NetworkManager.EndPacketAndSend(packetWriter, PacketChannel.Rpc, true, false);
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        MelonCoroutines.Start(CoWaitForNetworkObject(sender, packetReader));
    }

    private static IEnumerator CoWaitForNetworkObject(ReloadedClientData sender, PacketReader packetReader)
    {
        var packet = PacketReader.Get(packetReader.GetSubpacketBytes());
        var rpc = Message<ObjectRpcMessage>.Singleton.Deserialize(packet);
        float timeOut = 0f;

        try
        {
            while (ReloadedLobby.LobbyData != null && timeOut < 10f)
            {
                if (ReloadedLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(rpc.NetworkId, out var networkObj))
                {
                    if (!rpc.IsComponent)
                    {
                        MethodRpc.HandleRpc(networkObj, networkObj, sender, rpc.RpcId, packet);
                    }
                    else
                    {
                        var component = networkObj.NetworkComponents.ElementAtOrDefault(rpc.ComponentIndex);
                        MethodRpc.HandleRpc(component, component, sender, rpc.RpcId, packet);
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

    /// <summary>
    /// A readonly struct that wraps an <see cref="INetworkIdentifier"/> and captures its state at the time of construction.
    /// </summary>
    internal readonly struct CatchedNetworkIdentifier(INetworkIdentifier networkIdentifier) : INetworkIdentifier
    {
        /// <summary>
        /// Gets the identifier of the owner associated with the captured network identifier.
        /// </summary>
        public ID OwnerId { get; } = networkIdentifier.OwnerId;

        /// <summary>
        /// Gets the network identifier value captured from the source identifier.
        /// </summary>
        public NetworkIdentifier NetworkId { get; } = networkIdentifier.NetworkId;

        /// <summary>
        /// Gets the index of the network component captured from the source identifier.
        /// If the source is a <see cref="NetworkComponent"/>, its <see cref="NetworkComponent.Index"/> is used;
        /// otherwise, <see cref="ReplantedOnlineMod.Constants.Network.NULL_NETWORK_COMPONENT_INDEX"/> is returned.
        /// </summary>
        public int NetworkComponentIndex { get; } = networkIdentifier is NetworkComponent networkComponent ?
            networkComponent.Index : ReplantedOnlineMod.Constants.Network.NULL_NETWORK_COMPONENT_INDEX;
    }
}
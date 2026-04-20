using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Packet;

namespace ReplantedOnline.Interfaces.Network;

/// <summary>
/// Defines the contract for handling Remote Procedure Calls (RPCs) in ReplantedOnline.
/// Provides a structured framework for processing network commands between clients.
/// </summary>
internal interface IRPC
{
    /// <summary>
    /// Gets the RPC type that this handler is responsible for processing.
    /// </summary>
    /// <value>The <see cref="RpcType"/> that this handler will respond to.</value>
    RpcType Type { get; }

    /// <summary>
    /// Processes an incoming RPC packet from a network client.
    /// </summary>
    /// <param name="sender">The client that sent the RPC request.</param>
    /// <param name="packetReader">The packet reader containing the RPC data to process.</param>
    void Handle(ReplantedClientData sender, PacketReader packetReader);

    /// <summary>
    /// Dispatches an incoming RPC to the appropriate handler based on the RPC type.
    /// </summary>
    /// <param name="rpc">The type of RPC to handle.</param>
    /// <param name="sender">The client that sent the RPC request.</param>
    /// <param name="packetReader">The packet reader containing the RPC data.</param>
    internal static void HandleRpc(RpcType rpc, ReplantedClientData sender, PacketReader packetReader)
    {
        foreach (var handler in RegisterRPC.Instances)
        {
            if (handler.Type != rpc) continue;
            handler.Handle(sender, packetReader);

            break;
        }
    }
}
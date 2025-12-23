using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Network.Packet;

namespace ReplantedOnline.Network.RPC;

/// <summary>
/// Abstract base class for handling Remote Procedure Calls (RPCs) in ReplantedOnline.
/// Provides a structured framework for processing network commands between clients.
/// </summary>
internal abstract class RPCHandler
{
    /// <summary>
    /// Gets the RPC type that this handler is responsible for processing.
    /// </summary>
    /// <value>The <see cref="RpcType"/> that this handler will respond to.</value>
    internal abstract RpcType Rpc { get; }

    /// <summary>
    /// Processes an incoming RPC packet from a network client.
    /// </summary>
    /// <param name="sender">The client that sent the RPC request.</param>
    /// <param name="packetReader">The packet reader containing the RPC data to process.</param>
    internal abstract void Handle(SteamNetClient sender, PacketReader packetReader);

    /// <summary>
    /// Dispatches an incoming RPC to the appropriate handler based on the RPC type.
    /// </summary>
    /// <param name="rpc">The type of RPC to handle.</param>
    /// <param name="sender">The client that sent the RPC request.</param>
    /// <param name="packetReader">The packet reader containing the RPC data.</param>
    internal static void HandleRpc(RpcType rpc, SteamNetClient sender, PacketReader packetReader)
    {
        foreach (var handler in RegisterRPCHandler.Instances)
        {
            if (handler.Rpc != rpc) continue;
            handler.Handle(sender, packetReader);

            break;
        }
    }
}
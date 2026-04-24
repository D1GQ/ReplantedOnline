using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Packet;

namespace ReplantedOnline.Interfaces.Network;

/// <summary>
/// Defines the contract for handling Remote Procedure Calls (RPCs).
/// Provides a structured framework for processing network commands between clients.
/// </summary>
internal interface IRpc
{
    /// <summary>
    /// Gets the RPC type that this handler is responsible for processing.
    /// </summary>
    /// <value>The <see cref="RpcType"/> that this handler will respond to.</value>
    RpcType Rpc { get; }

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
        foreach (var handler in RegisterRpc.Instances)
        {
            if (handler.Rpc != rpc) continue;
            handler.Handle(sender, packetReader);

            break;
        }
    }
}

/// <summary>
/// Represents an RPC handler that can dispatch messages without additional arguments.
/// Extends <see cref="IRpc"/> with a parameterless send capability.
/// </summary>
internal interface IRpcDispatcher : IRpc
{
    /// <summary>
    /// Sends the RPC message without any additional arguments.
    /// </summary>
    void Send();
}

/// <summary>
/// Represents an RPC handler that can dispatch messages with a single additional argument.
/// </summary>
/// <typeparam name="Arg1">The type of the argument used when sending the RPC message.</typeparam>
internal interface IRpcDispatcher<Arg1> : IRpc
{
    /// <summary>
    /// Sends the RPC message with the specified argument.
    /// </summary>
    /// <param name="arg1">The argument containing data to send with the RPC message.</param>
    void Send(Arg1 arg1);
}

/// <summary>
/// Represents an RPC handler that can dispatch messages with two additional arguments.
/// </summary>
/// <typeparam name="Arg1">The type of the first argument used when sending the RPC message.</typeparam>
/// <typeparam name="Arg2">The type of the second argument used when sending the RPC message.</typeparam>
internal interface IRpcDispatcher<Arg1, Arg2> : IRpc
{
    /// <summary>
    /// Sends the RPC message with the specified arguments.
    /// </summary>
    /// <param name="arg1">The first argument containing data to send with the RPC message.</param>
    /// <param name="arg2">The second argument containing data to send with the RPC message.</param>
    void Send(Arg1 arg1, Arg2 arg2);
}
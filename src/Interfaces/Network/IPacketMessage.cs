using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Network.Reloaded.Serialization;

namespace ReplantedOnline.Interfaces.Network;

/// <summary>
/// Defines the contract for handling Network Packets.
/// </summary>
internal interface IBasePacketMessage
{
    /// <summary>
    /// Processes an incoming network packet from a connected client.
    /// </summary>
    /// <param name="sender">The client that sent the packet.</param>
    /// <param name="packetReader">The packet reader containing the raw packet data
    /// to be deserialized and processed by the handler.</param>
    /// <param name="local">Whether if this packet is from the local client.</param>
    void Receive(ReloadedClientData sender, PacketReader packetReader, bool local);

    /// <summary>
    /// Dispatches an incoming packet to the appropriate registered handler based on its tag.
    /// </summary>
    /// <param name="handlerType">The packet tag identifying the type of packet received.</param>
    /// <param name="sender">The client that sent the packet.</param>
    /// <param name="packetReader">The packet reader containing the packet data.</param>
    /// <param name="local">Whether if this packet is from the local client.</param>
    /// <returns>
    /// <c>true</c> if a handler was found and successfully processed the packet;
    /// otherwise, <c>false</c> if no handler is registered for the specified tag.
    /// </returns>
    internal static bool HandlePacket(PacketType handlerType, ReloadedClientData sender, PacketReader packetReader, bool local)
    {
        var dispatcher = RegisterPacketHandler.GetInstanceFromLookup(handlerType);
        if (dispatcher != null)
        {
            dispatcher.Receive(sender, packetReader, local);
            return true;
        }

        return false;
    }
}

/// <summary>
/// Represents a packet handler that can dispatch messages without additional arguments.
/// Extends <see cref="IBasePacketMessage"/> with a parameterless send capability.
/// </summary>
internal interface IPacketMessage : IBasePacketMessage
{
    /// <summary>
    /// Sends the packet message without any additional arguments.
    /// </summary>
    void Send();
}

/// <summary>
/// Represents a packet handler that can dispatch messages with a single additional argument.
/// </summary>
/// <typeparam name="Arg1">The type of the argument used when sending the packet message.</typeparam>
internal interface IPacketMessage<Arg1> : IBasePacketMessage
{
    /// <summary>
    /// Sends the packet message with the specified argument.
    /// </summary>
    /// <param name="arg1">The argument containing data to send with the packet message.</param>
    void Send(Arg1 arg1);
}

/// <summary>
/// Represents a packet handler that can dispatch messages with two additional arguments.
/// </summary>
/// <typeparam name="Arg1">The type of the first argument used when sending the packet message.</typeparam>
/// <typeparam name="Arg2">The type of the second argument used when sending the packet message.</typeparam>
internal interface IPacketMessage<Arg1, Arg2> : IBasePacketMessage
{
    /// <summary>
    /// Sends the packet message with the specified arguments.
    /// </summary>
    /// <param name="arg1">The first argument containing data to send with the packet message.</param>
    /// <param name="arg2">The second argument containing data to send with the packet message.</param>
    void Send(Arg1 arg1, Arg2 arg2);
}

/// <summary>
/// Represents a packet handler that can dispatch messages with three additional arguments.
/// </summary>
/// <typeparam name="Arg1">The type of the first argument used when sending the packet message.</typeparam>
/// <typeparam name="Arg2">The type of the second argument used when sending the packet message.</typeparam>
/// <typeparam name="Arg3">The type of the third argument used when sending the packet message.</typeparam>
internal interface IPacketMessage<Arg1, Arg2, Arg3> : IBasePacketMessage
{
    /// <summary>
    /// Sends the packet message with the specified arguments.
    /// </summary>
    /// <param name="arg1">The first argument containing data to send with the packet message.</param>
    /// <param name="arg2">The second argument containing data to send with the packet message.</param>
    /// <param name="arg3">The third argument containing data to send with the packet message.</param>
    void Send(Arg1 arg1, Arg2 arg2, Arg3 arg3);
}

/// <summary>
/// Represents a packet handler that can dispatch messages with four additional arguments.
/// </summary>
/// <typeparam name="Arg1">The type of the first argument used when sending the packet message.</typeparam>
/// <typeparam name="Arg2">The type of the second argument used when sending the packet message.</typeparam>
/// <typeparam name="Arg3">The type of the third argument used when sending the packet message.</typeparam>
/// <typeparam name="Arg4">The type of the fourth argument used when sending the packet message.</typeparam>
internal interface IPacketMessage<Arg1, Arg2, Arg3, Arg4> : IBasePacketMessage
{
    /// <summary>
    /// Sends the packet message with the specified arguments.
    /// </summary>
    /// <param name="arg1">The first argument containing data to send with the packet message.</param>
    /// <param name="arg2">The second argument containing data to send with the packet message.</param>
    /// <param name="arg3">The third argument containing data to send with the packet message.</param>
    /// <param name="arg4">The fourth argument containing data to send with the packet message.</param>
    void Send(Arg1 arg1, Arg2 arg2, Arg3 arg3, Arg4 arg4);
}
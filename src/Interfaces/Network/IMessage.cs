using ReplantedOnline.Network.Packet;

namespace ReplantedOnline.Interfaces.Network;

/// <summary>
/// Represents the base interface for all network messages.
/// </summary>
internal interface IMessage
{

}

/// <summary>
/// Represents a network message that can be serialized and deserialized without additional arguments.
/// </summary>
/// <typeparam name="Return">The message type that implements <see cref="IMessage"/> and will be returned during deserialization.</typeparam>
internal interface IMessage<Return> : IMessage where Return : IMessage
{
    /// <summary>
    /// Serializes the message data into the specified packet writer.
    /// </summary>
    /// <param name="packetWriter">The packet writer to which the serialized data will be written. Cannot be null.</param>
    void Serialize(PacketWriter packetWriter);

    /// <summary>
    /// Deserializes a message from the specified packet reader.
    /// </summary>
    /// <param name="packetReader">The packet reader from which to read the message data. Must be positioned at the start of a valid message.</param>
    /// <returns>The deserialized message instance of type <typeparamref name="Return"/>.</returns>
    Return Deserialize(PacketReader packetReader);
}

/// <summary>
/// Represents a network message that can be serialized with a single additional argument.
/// </summary>
/// <typeparam name="Return">The message type that implements <see cref="IMessage"/> and will be returned during deserialization.</typeparam>
/// <typeparam name="Arg1">The type of the first argument used during serialization.</typeparam>
internal interface IMessage<Return, Arg1> : IMessage where Return : IMessage
{
    /// <summary>
    /// Serializes the message data into the specified packet writer using the provided argument.
    /// </summary>
    /// <param name="arg1">The first argument containing data required for serialization.</param>
    /// <param name="packetWriter">The packet writer to which the serialized data will be written. Cannot be null.</param>
    void Serialize(Arg1 arg1, PacketWriter packetWriter);

    /// <summary>
    /// Deserializes a message from the specified packet reader.
    /// </summary>
    /// <param name="packetReader">The packet reader from which to read the message data. Must be positioned at the start of a valid message.</param>
    /// <returns>The deserialized message instance of type <typeparamref name="Return"/>.</returns>
    Return Deserialize(PacketReader packetReader);
}

/// <summary>
/// Represents a network message that can be serialized with two additional arguments.
/// </summary>
/// <typeparam name="Return">The message type that implements <see cref="IMessage"/> and will be returned during deserialization.</typeparam>
/// <typeparam name="Arg1">The type of the first argument used during serialization.</typeparam>
/// <typeparam name="Arg2">The type of the second argument used during serialization.</typeparam>
internal interface IMessage<Return, Arg1, Arg2> : IMessage where Return : IMessage
{
    /// <summary>
    /// Serializes the message data into the specified packet writer using the provided arguments.
    /// </summary>
    /// <param name="arg1">The first argument containing data required for serialization.</param>
    /// <param name="arg2">The second argument containing data required for serialization.</param>
    /// <param name="packetWriter">The packet writer to which the serialized data will be written. Cannot be null.</param>
    void Serialize(Arg1 arg1, Arg2 arg2, PacketWriter packetWriter);

    /// <summary>
    /// Deserializes a message from the specified packet reader.
    /// </summary>
    /// <param name="packetReader">The packet reader from which to read the message data. Must be positioned at the start of a valid message.</param>
    /// <returns>The deserialized message instance of type <typeparamref name="Return"/>.</returns>
    Return Deserialize(PacketReader packetReader);
}
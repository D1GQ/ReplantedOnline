using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Server.Packet;

namespace ReplantedOnline.Network.Server.PacketHandler;

/// <summary>
/// Abstract base class for handling network packets in ReplantedOnline.
/// Implements the handler pattern for processing different types of network packets
/// received from connected clients.
/// </summary>
internal abstract class BasePacketHandler
{
    /// <summary>
    /// Gets the packet tag that this handler is responsible for processing.
    /// </summary>
    /// <value>
    /// The <see cref="PacketTag"/> enumeration value that uniquely identifies
    /// the packet type this handler will process.
    /// </value>
    internal abstract PacketTag Tag { get; }

    /// <summary>
    /// Processes an incoming network packet from a connected client.
    /// </summary>
    /// <param name="sender">The client that sent the packet. Contains Steam ID,
    /// connection state, and other client-specific information.</param>
    /// <param name="packetReader">The packet reader containing the raw packet data
    /// to be deserialized and processed by the handler.</param>
    /// <remarks>
    /// Implementations should handle deserialization, validation, and any necessary
    /// game state modifications based on the packet contents.
    /// </remarks>
    internal abstract void Handle(NetClient sender, PacketReader packetReader);

    /// <summary>
    /// Dispatches an incoming packet to the appropriate registered handler based on its tag.
    /// </summary>
    /// <param name="tag">The packet tag identifying the type of packet received.</param>
    /// <param name="sender">The client that sent the packet.</param>
    /// <param name="packetReader">The packet reader containing the packet data.</param>
    /// <returns>
    /// <c>true</c> if a handler was found and successfully processed the packet;
    /// otherwise, <c>false</c> if no handler is registered for the specified tag.
    /// </returns>
    internal static bool HandlePacket(PacketTag tag, NetClient sender, PacketReader packetReader)
    {
        foreach (var dispatcher in RegisterPacketHandler.Instances)
        {
            if (dispatcher.Tag != tag) continue;
            dispatcher.Handle(sender, packetReader);
            return true;
        }

        return false;
    }
}
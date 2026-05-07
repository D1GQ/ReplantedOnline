using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Packet;

namespace ReplantedOnline.Interfaces.Network;

/// <summary>
/// Defines the contract for handling network packets in ReplantedOnline.
/// Implements the handler pattern for processing different types of network packets
/// received from connected clients.
/// </summary>
internal interface IPacketHandler
{
    /// <summary>
    /// Processes an incoming network packet from a connected client.
    /// </summary>
    /// <param name="sender">The client that sent the packet.</param>
    /// <param name="packetReader">The packet reader containing the raw packet data
    /// to be deserialized and processed by the handler.</param>
    /// <param name="local">Whether if this packet is from the local client.</param>
    void Handle(ReplantedClientData sender, PacketReader packetReader, bool local);

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
    internal static bool HandlePacket(PacketHandlerType handlerType, ReplantedClientData sender, PacketReader packetReader, bool local)
    {
        var dispatcher = RegisterPacketHandler.GetInstanceFromLookup(handlerType);
        if (dispatcher != null)
        {
            dispatcher.Handle(sender, packetReader, local);
            return true;
        }

        return false;
    }
}
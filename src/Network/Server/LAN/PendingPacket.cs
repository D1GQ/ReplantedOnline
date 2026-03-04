using ReplantedOnline.Structs;

namespace ReplantedOnline.Network.Server.LAN;

/// <summary>
/// Represents a packet waiting to be processed from the network queue.
/// Used to store incoming packets before they are read by the game thread.
/// </summary>
internal sealed class PendingPacket
{
    /// <summary>
    /// Gets or sets the raw packet data bytes.
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the client that sent this packet.
    /// </summary>
    public ID SenderId { get; set; }

    /// <summary>
    /// Gets or sets the size of the packet data in bytes.
    /// </summary>
    public uint Size { get; set; }
}
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Network.Reloaded.Serialization;

namespace ReplantedOnline.Structs.Network;

/// <summary>
/// Represents a packet that has been buffered for later sending, including its data, target client, and channel information.
/// </summary>
/// <param name="packetWriter">The packet writer containing the packet data.</param>
/// <param name="packetChannel">The channel on which the packet will be sent.</param>
/// <param name="targetId">The ID of the target client.</param>
/// <param name="isLocal">Indicates whether the packet is intended for the local client.</param>
internal readonly struct BufferedPacket(PacketWriter packetWriter, PacketChannel packetChannel, ID targetId, bool isLocal)
{
    /// <summary>
    /// The ID of the target client for this buffered packet.
    /// </summary>
    internal readonly ID TargetId = targetId;

    /// <summary>
    /// The network channel on which this buffered packet will be sent.
    /// </summary>
    internal readonly PacketChannel PacketChannel = packetChannel;

    /// <summary>
    /// The raw byte array representing the packet data to be sent.
    /// </summary>
    internal readonly byte[] PacketBytes = packetWriter.GetBytes();

    /// <summary>
    /// Indicates whether this buffered packet is intended for the local client.
    /// </summary>
    internal readonly bool IsLocal = isLocal;
}

using ReplantedOnline.Modules.Modded;
using ReplantedOnline.Structs.Network;

namespace ReplantedOnline.Network.Reloaded.Serialization;

/// <summary>
/// Provides a pooled buffer packets, handling packet data storage and memory management.
/// </summary>
internal sealed class PacketBuffer
{
    private static readonly PoolableObjects<PacketBuffer> _pool = new(5);

    /// <summary>
    /// The size of the packet data in bytes.
    /// </summary>
    internal uint Size;

    /// <summary>
    /// The ID of the peer that sent this packet.
    /// </summary>
    internal ID ClientId;

    /// <summary>
    /// The packet data.
    /// </summary>
    internal byte[]? Data;

    /// <summary>
    /// Retrieves a P2PPacketBuffer instance from the pool or creates a new one if the pool is empty.
    /// </summary>
    /// <returns>A P2PPacketBuffer instance ready for use.</returns>
    internal static PacketBuffer Get(uint messageSize)
    {
        var p2pPacket = _pool.Get();
        p2pPacket.EnsureCapacity(messageSize);
        p2pPacket.Size = messageSize;
        return p2pPacket;
    }

    /// <summary>
    /// Ensures the Data buffer has at least the specified capacity.
    /// </summary>
    /// <param name="requiredSize">The minimum required capacity in bytes.</param>
    private void EnsureCapacity(uint requiredSize)
    {
        if (requiredSize == 0)
        {
            Data = [];
            return;
        }

        if (Data == null || Data.Length < requiredSize)
        {
            Data = new byte[requiredSize];
        }
    }

    /// <summary>
    /// Recycles this P2PPacketBuffer instance back to the pool for reuse.
    /// </summary>
    internal void Recycle()
    {
        Size = 0;
        ClientId = 0;
        Data = null;
        _pool.Release(this);
    }
}
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ReplantedOnline.Structs;

namespace ReplantedOnline.Network.Packet;

/// <summary>
/// Provides a pooled buffer for P2P network packets, handling packet data storage and memory management.
/// Uses object pooling to reduce GC pressure when processing frequent network packets.
/// </summary>
internal sealed class P2PPacketBuffer
{
    private static readonly Queue<P2PPacketBuffer> _pool = [];
    private const int MAX_POOL_SIZE = 5;
    internal static int AmountInUse;

    /// <summary>
    /// The size of the packet data in bytes.
    /// </summary>
    public uint Size;

    /// <summary>
    /// The ID of the peer that sent this packet.
    /// </summary>
    public ID ClientId;

    /// <summary>
    /// The packet data stored in an Il2Cpp-compatible byte array.
    /// </summary>
    public Il2CppStructArray<byte> Data;

    /// <summary>
    /// Optional raw byte array for LAN packets, used when the packet data is received directly as a byte array instead of an Il2CppStructArray.
    /// </summary>
    public byte[] LanData;

    /// <summary>
    /// Retrieves a P2PPacketBuffer instance from the pool or creates a new one if the pool is empty.
    /// </summary>
    /// <returns>A P2PPacketBuffer instance ready for use.</returns>
    internal static P2PPacketBuffer Get(uint messageSize)
    {
        AmountInUse++;
        var p2pPacket = _pool.Count > 0 ? _pool.Dequeue() : new P2PPacketBuffer();
        p2pPacket.EnsureCapacity(messageSize);
        p2pPacket.Size = messageSize;
        return p2pPacket;
    }

    /// <summary>
    /// Ensures the Data buffer has at least the specified capacity.
    /// Reallocates the buffer if the current capacity is insufficient.
    /// </summary>
    /// <param name="requiredSize">The minimum required capacity in bytes.</param>
    private void EnsureCapacity(uint requiredSize)
    {
        if (requiredSize == 0)
        {
            Data = new Il2CppStructArray<byte>(1);
            return;
        }

        if (Data == null || Data.Length < requiredSize)
        {
            Data = new Il2CppStructArray<byte>((int)requiredSize);
        }
    }

    /// <summary>
    /// Converts the packet data to a managed byte array containing only the actual packet bytes.
    /// </summary>
    /// <returns>A byte array containing the packet data, or an empty array if no data is present.</returns>
    internal byte[] ToByteArray()
    {
        if (Size == 0)
            return [];

        if (LanData != null)
        {
            return LanData;
        }

        if (Data == null)
            return [];

        var result = new byte[Size];
        for (int i = 0; i < Size; i++)
        {
            result[i] = Data[i];
        }
        return result;
    }

    /// <summary>
    /// Recycles this P2PPacketBuffer instance back to the pool for reuse.
    /// Resets the buffer state and either pools the instance or cleans up resources if pool is full.
    /// </summary>
    internal void Recycle()
    {
        AmountInUse--;
        Size = 0;
        ClientId = 0;

        if (_pool.Count < MAX_POOL_SIZE)
        {
            _pool.Enqueue(this);
        }
        else
        {
            Data = null;
        }
    }
}
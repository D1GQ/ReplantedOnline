using ReplantedOnline.Enums;
using ReplantedOnline.Network.Object;
using System.Text;
using UnityEngine;

namespace ReplantedOnline.Network.Packet;

/// <summary>
/// Provides a pooled packet writer for efficient network packet construction.
/// Handles writing various data types to a byte buffer with object pooling to reduce GC pressure.
/// </summary>
internal sealed class PacketWriter
{
    private readonly List<byte> _data = [];
    private static readonly Queue<PacketWriter> _pool = [];
    private const int MAX_POOL_SIZE = 100;
    internal static int AmountInUse;

    /// <summary>
    /// Gets the current packet data as a byte array.
    /// </summary>
    /// <returns>A byte array containing the packet data.</returns>
    internal byte[] GetBytes() => _data.ToArray();

    /// <summary>
    /// Gets the current length of the packet data in bytes.
    /// </summary>
    internal int Length => _data.Count;

    /// <summary>
    /// Retrieves a PacketWriter instance from the pool or creates a new one if the pool is empty.
    /// </summary>
    /// <returns>A PacketWriter instance ready for use.</returns>
    internal static PacketWriter Get()
    {
        AmountInUse++;
        return _pool.Count > 0 ? _pool.Dequeue() : new PacketWriter();
    }

    /// <summary>
    /// Writes an networkclass.
    /// </summary>
    internal void WriteNetworkClass(NetworkClass networkClass)
    {
        WriteUInt(networkClass.NetworkId);
    }

    /// <summary>
    /// Writes another packet's contents into this packet writer.
    /// </summary>
    /// <param name="packetWriter">The packet writer whose contents will be written.</param>
    internal void WritePacket(PacketWriter packetWriter)
    {
        _data.AddRange(packetWriter.GetBytes());
    }

    /// <summary>
    /// Writes a Vector2 to the packet as two consecutive float values (X and Y).
    /// </summary>
    /// <param name="value">The Vector2 value to write.</param>
    internal void WriteVector2(Vector2 value)
    {
        _data.AddRange(BitConverter.GetBytes(value.x));
        _data.AddRange(BitConverter.GetBytes(value.y));
    }

    /// <summary>
    /// Writes a string to the packet with UTF-8 encoding, prefixed by its length.
    /// </summary>
    /// <param name="value">The string value to write.</param>
    internal void WriteString(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        WriteInt(bytes.Length);
        _data.AddRange(bytes);
    }

    /// <summary>
    /// Adds a packet tag to identify the packet type.
    /// </summary>
    /// <param name="tag">The packet tag to write.</param>
    internal void AddTag(PacketTag tag)
    {
        WriteByte((byte)tag);
    }

    /// <summary>
    /// Writes a 4-byte signed integer to the packet.
    /// </summary>
    /// <param name="value">The integer value to write.</param>
    internal void WriteInt(int value)
    {
        _data.AddRange(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Writes a 4-byte unsigned integer to the packet.
    /// </summary>
    /// <param name="value">The unsigned integer value to write.</param>
    internal void WriteUInt(uint value)
    {
        _data.AddRange(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Writes a 4-byte floating-point value to the packet.
    /// </summary>
    /// <param name="value">The float value to write.</param>
    internal void WriteFloat(float value)
    {
        _data.AddRange(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Writes a boolean value to the packet as a single byte (1 for true, 0 for false).
    /// </summary>
    /// <param name="value">The boolean value to write.</param>
    internal void WriteBool(bool value)
    {
        _data.Add(value ? (byte)1 : (byte)0);
    }

    /// <summary>
    /// Writes a single byte to the packet.
    /// </summary>
    /// <param name="value">The byte value to write.</param>
    internal void WriteByte(byte value)
    {
        _data.Add(value);
    }

    /// <summary>
    /// Writes a byte array to the packet, prefixed by its length.
    /// </summary>
    /// <param name="bytes">The byte array to write.</param>
    internal void WriteBytes(byte[] bytes)
    {
        WriteInt(bytes.Length);
        _data.AddRange(bytes);
    }

    /// <summary>
    /// Writes an 8-byte signed integer to the packet.
    /// </summary>
    /// <param name="value">The long value to write.</param>
    internal void WriteLong(long value)
    {
        _data.AddRange(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Writes an 8-byte unsigned integer to the packet.
    /// </summary>
    /// <param name="value">The unsigned long value to write.</param>
    internal void WriteULong(ulong value)
    {
        _data.AddRange(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Writes an 8-byte double-precision floating-point value to the packet.
    /// </summary>
    /// <param name="value">The double value to write.</param>
    internal void WriteDouble(double value)
    {
        _data.AddRange(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Recycles this PacketWriter instance back to the pool for reuse.
    /// Clears the current data and adds the instance to the pool if under maximum size.
    /// </summary>
    internal void Recycle()
    {
        AmountInUse--;
        _data.Clear();

        if (_pool.Count < MAX_POOL_SIZE)
            _pool.Enqueue(this);
    }

    /// <summary>
    /// CLear all data in the PacketWriter
    /// </summary>
    internal void Clear()
    {
        _data.Clear();
    }
}
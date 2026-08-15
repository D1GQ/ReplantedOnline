using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Modded;
using System.Text;
using UnityEngine;

namespace ReplantedOnline.Network.Reloaded.Serialization;

/// <summary>
/// Provides a pooled packet writer for efficient network packet construction.
/// Handles writing various data types to a byte buffer with object pooling to reduce GC pressure.
/// </summary>
internal sealed class PacketWriter : IPacket
{
    private List<byte> _data = [];
    private static readonly PoolableObjects<PacketWriter> _pool = new(10);
    private readonly Stack<int> _messageStarts = new();

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
        return _pool.Get();
    }

    /// <summary>
    /// Retrieves a PacketWriter instance from the pool or creates a new one, initialized with the provided data.
    /// </summary>
    /// <param name="data">The byte array containing initial packet data to write from.</param>
    /// <returns>A PacketWriter instance initialized with the specified data.</returns>
    internal static PacketWriter Get(byte[] data)
    {
        var writer = _pool.Get();
        writer._data = [.. data];
        return writer;
    }

    /// <summary>
    /// Starts a sub-packet with the specified tag flag.
    /// </summary>
    /// <param name="tag">The tag flag for the sub-packet.</param>
    internal void StartSubpacket(byte tag)
    {
        var messageStart = _data.Count;
        _messageStarts.Push(messageStart);

        _data.Add(0);
        _data.Add(0);
        _data.Add(tag);
    }

    /// <summary>
    /// Ends the current sub-packet, updating its length.
    /// </summary>
    internal void EndSubpacket()
    {
        if (_messageStarts.Count == 0)
            throw new InvalidOperationException("No subpacket to end");

        var lastMessageStart = _messageStarts.Pop();
        int payloadEnd = _data.Count;
        int payloadLength = payloadEnd - lastMessageStart - 3;

        if (payloadLength < 0)
            throw new InvalidOperationException("Subpacket payload length cannot be negative");

        if (payloadLength > ushort.MaxValue)
            throw new InvalidOperationException($"Subpacket payload length exceeds maximum: {payloadLength} > {ushort.MaxValue}");

        ushort length = (ushort)payloadLength;
        _data[lastMessageStart] = (byte)length;
        _data[lastMessageStart + 1] = (byte)(length >> 8);
    }

    /// <summary>
    /// Cancels the current sub-packet, removing it from the buffer.
    /// </summary>
    internal void CancelSubpacket()
    {
        if (_messageStarts.Count == 0)
            throw new InvalidOperationException("No subpacket to cancel");

        var messageStart = _messageStarts.Pop();
        _data.RemoveRange(messageStart, _data.Count - messageStart);
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
    /// Writes a 4-byte signed integer to the packet using 7-bit packed encoding (LEB128).
    /// </summary>
    /// <param name="value">The integer value to write.</param>
    internal void WritePackedInt(int value)
    {
        if (value < 0)
            throw new ArgumentException("Use WriteInt() for negative values");

        WritePackedUInt((uint)value);
    }

    /// <summary>
    /// Writes a 4-byte unsigned integer to the packet using 7-bit packed encoding (LEB128).
    /// </summary>
    /// <param name="value">The unsigned integer value to write.</param>
    internal void WritePackedUInt(uint value)
    {
        do
        {
            byte b = (byte)(value & 0xFF);
            if (value >= 0x80)
            {
                b |= 0x80;
            }

            WriteByte(b);
            value >>= 7;
        } while (value > 0);
    }

    /// <summary>
    /// Writes a float value packed into a short.
    /// </summary>
    /// <param name="value">The float value to write.</param>
    /// <param name="scale">The scale factor to multiply the value by.</param>
    internal void WritePackedFloat(float value, float scale)
    {
        if (scale == 0)
            throw new ArgumentException("Scale cannot be zero", nameof(scale));

        short packedValue = (short)Mathf.Clamp(Mathf.RoundToInt(value * scale), short.MinValue, short.MaxValue);
        WriteShort(packedValue);
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
        WritePackedInt(bytes.Length);
        _data.AddRange(bytes);
    }

    /// <summary>
    /// Writes a 2-byte signed integer (short) to the packet.
    /// </summary>
    /// <param name="value">The short value to write.</param>
    internal void WriteShort(short value)
    {
        _data.AddRange(BitConverter.GetBytes(value));
    }

    /// <summary>
    /// Writes a 2-byte unsigned integer (ushort) to the packet.
    /// </summary>
    /// <param name="value">The ushort value to write.</param>
    internal void WriteUShort(ushort value)
    {
        _data.AddRange(BitConverter.GetBytes(value));
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
    /// Adds a byte array to the packet buffer.
    /// </summary>
    /// <param name="bytes">The byte array to add.</param>
    internal void AddBytesToBuffer(byte[] bytes)
    {
        _data.AddRange(bytes);
    }

    /// <summary>
    /// Recycles this PacketWriter instance back to the pool for reuse.
    /// Clears the current data and adds the instance to the pool if under maximum size.
    /// </summary>
    internal void Recycle()
    {
        _data.Clear();
        _messageStarts.Clear();
        _pool.Release(this);
    }

    /// <summary>
    /// CLear all data in the PacketWriter
    /// </summary>
    internal void Clear()
    {
        _data.Clear();
        _messageStarts.Clear();
    }

    /// <inheritdoc/>
    public byte[] GetBytes()
    {
        return [.. _data];
    }

    /// <inheritdoc/>
    public byte[] GetSubpacketBytes()
    {
        if (_messageStarts.Count == 0)
            return [];

        int start = _messageStarts.Peek() + 3;
        int length = _data.Count - start;

        if (length <= 0)
            return [];

        byte[] result = new byte[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = _data[start + i];
        }
        return result;
    }
}
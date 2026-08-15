using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules.Modded;
using System.Text;

namespace ReplantedOnline.Network.Reloaded.Serialization;

/// <summary>
/// Provides a pooled packet reader for efficient network packet parsing.
/// Handles reading various data types from a byte buffer with object pooling to reduce GC pressure.
/// </summary>
internal sealed class PacketReader : IPacket
{
    private byte[] _data = [];
    private int _position = 0;
    private int _end = 0;
    private static readonly PoolableObjects<PacketReader> _pool = new(10);

    /// <summary>
    /// Gets the number of bytes remaining to be read in the packet.
    /// </summary>
    internal int Remaining => _end - _position;

    /// <summary>
    /// Gets the tag identifier for the current subpacket.
    /// </summary>
    internal byte SubTag { get; private set; }

    /// <summary>
    /// Retrieves a PacketReader instance from the pool or creates a new one, initialized with the provided data.
    /// </summary>
    /// <param name="data">The byte array containing packet data to read from.</param>
    /// <returns>A PacketReader instance ready for reading the provided data.</returns>
    internal static PacketReader Get(byte[] data)
    {
        var reader = _pool.Get();
        reader._data = data;
        reader._position = 0;
        reader._end = data.Length;
        reader.SubTag = 0;
        return reader;
    }

    /// <summary>
    /// Moves to the next sub-packet in the current packet stream and returns a new PacketReader for its payload.
    /// </summary>
    /// <returns>A new PacketReader positioned at the sub-packet payload, or null if no more sub-packets are available.</returns>
    internal PacketReader NextSubpacket()
    {
        if (Remaining < 3)
            throw new InvalidDataException("Not enough bytes remaining to read Subpacket.");

        ushort length = (ushort)(_data[_position] | (_data[_position + 1] << 8));
        byte tag = _data[_position + 2];

        int payloadStart = _position + 3;
        int payloadEnd = payloadStart + length;

        if (payloadEnd > _end)
            throw new InvalidDataException("Subpacket extends beyond parent.");

        var child = _pool.Get();
        child._data = _data;
        child._position = payloadStart;
        child._end = payloadEnd;
        child.SubTag = tag;

        _position = payloadEnd;

        return child;
    }

    /// <summary>
    /// Reads a string from the packet, expecting length-prefixed UTF-8 encoding.
    /// </summary>
    /// <returns>The decoded string value.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when there's not enough data to read the string.</exception>
    internal string ReadString()
    {
        int length = ReadInt();
        if (Remaining < length)
            throw new IndexOutOfRangeException("Not enough data to read string");

        string result = Encoding.UTF8.GetString(_data, _position, length);
        _position += length;
        return result;
    }

    /// <summary>
    /// Reads a 4-byte signed integer from the packet.
    /// </summary>
    /// <returns>The integer value.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when there's not enough data to read an integer.</exception>
    internal int ReadInt()
    {
        if (Remaining < 4)
            throw new IndexOutOfRangeException("Not enough data to read int");

        int result = BitConverter.ToInt32(_data, _position);
        _position += 4;
        return result;
    }

    /// <summary>
    /// Reads a 4-byte unsigned integer from the packet.
    /// </summary>
    /// <returns>The unsigned integer value.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when there's not enough data to read an unsigned integer.</exception>
    internal uint ReadUInt()
    {
        if (Remaining < 4)
            throw new IndexOutOfRangeException("Not enough data to read uint");

        uint result = BitConverter.ToUInt32(_data, _position);
        _position += 4;
        return result;
    }

    /// <summary>
    /// Reads a variable-length 32-bit signed integer encoded with 7-bit packing (LEB128).
    /// </summary>
    /// <returns>The decoded signed integer value.</returns>
    /// <exception cref="InvalidDataException">Thrown when the encoded length exceeds the remaining packet data.</exception>
    public int ReadPackedInt()
    {
        return (int)ReadPackedUInt();
    }

    /// <summary>
    /// Reads a variable-length 32-bit unsigned integer encoded with 7-bit packing (LEB128).
    /// </summary>
    /// <returns>The decoded unsigned integer value.</returns>
    /// <exception cref="InvalidDataException">Thrown when the encoded length exceeds the remaining packet data.</exception>
    public uint ReadPackedUInt()
    {
        bool readMore = true;
        int shift = 0;
        uint output = 0;

        while (readMore)
        {
            if (Remaining < 1) throw new InvalidDataException($"Read length is longer than message length.");

            byte b = ReadByte();
            if (b >= 0x80)
            {
                readMore = true;
                b ^= 0x80;
            }
            else
            {
                readMore = false;
            }

            output |= (uint)(b << shift);
            shift += 7;
        }

        return output;
    }

    /// <summary>
    /// Reads a float value that was packed into a short.
    /// </summary>
    /// <returns>The unpacked float value.</returns>
    internal float ReadPackedFloat(float scale)
    {
        short packedValue = ReadShort();
        return packedValue / (float)scale;
    }

    /// <summary>
    /// Reads a 4-byte floating-point value from the packet.
    /// </summary>
    /// <returns>The float value.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when there's not enough data to read a float.</exception>
    internal float ReadFloat()
    {
        if (Remaining < 4)
            throw new IndexOutOfRangeException("Not enough data to read float");

        float result = BitConverter.ToSingle(_data, _position);
        _position += 4;
        return result;
    }

    /// <summary>
    /// Reads a boolean value from the packet (1 byte: 1 for true, 0 for false).
    /// </summary>
    /// <returns>The boolean value.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when there's not enough data to read a boolean.</exception>
    internal bool ReadBool()
    {
        if (Remaining < 1)
            throw new IndexOutOfRangeException("Not enough data to read bool");

        return _data[_position++] == 1;
    }

    /// <summary>
    /// Reads a single byte from the packet.
    /// </summary>
    /// <returns>The byte value.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when there's not enough data to read a byte.</exception>
    internal byte ReadByte()
    {
        if (Remaining < 1)
            throw new IndexOutOfRangeException("Not enough data to read byte");

        return _data[_position++];
    }

    /// <summary>
    /// Reads a length-prefixed byte array from the packet.
    /// </summary>
    /// <returns>The byte array.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when there's not enough data to read the byte array.</exception>
    internal byte[] ReadBytes()
    {
        int length = ReadPackedInt();
        if (Remaining < length)
            throw new IndexOutOfRangeException("Not enough data to read bytes");

        byte[] result = new byte[length];
        Array.Copy(_data, _position, result, 0, length);
        _position += length;
        return result;
    }

    /// <summary>
    /// Reads a 2-byte signed integer (short) from the packet.
    /// </summary>
    /// <returns>The short value.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when there's not enough data to read a short.</exception>
    internal short ReadShort()
    {
        if (Remaining < 2)
            throw new IndexOutOfRangeException("Not enough data to read short");

        short result = BitConverter.ToInt16(_data, _position);
        _position += 2;
        return result;
    }

    /// <summary>
    /// Reads a 2-byte unsigned integer (ushort) from the packet.
    /// </summary>
    /// <returns>The ushort value.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when there's not enough data to read a ushort.</exception>
    internal ushort ReadUShort()
    {
        if (Remaining < 2)
            throw new IndexOutOfRangeException("Not enough data to read ushort");

        ushort result = BitConverter.ToUInt16(_data, _position);
        _position += 2;
        return result;
    }

    /// <summary>
    /// Reads an 8-byte signed integer from the packet.
    /// </summary>
    /// <returns>The long value.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when there's not enough data to read a long.</exception>
    internal long ReadLong()
    {
        if (Remaining < 8)
            throw new IndexOutOfRangeException("Not enough data to read long");

        long result = BitConverter.ToInt64(_data, _position);
        _position += 8;
        return result;
    }

    /// <summary>
    /// Reads an 8-byte unsigned integer from the packet.
    /// </summary>
    /// <returns>The unsigned long value.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when there's not enough data to read an unsigned long.</exception>
    internal ulong ReadULong()
    {
        if (Remaining < 8)
            throw new IndexOutOfRangeException("Not enough data to read ulong");

        ulong result = BitConverter.ToUInt64(_data, _position);
        _position += 8;
        return result;
    }

    /// <summary>
    /// Reads an 8-byte double-precision floating-point value from the packet.
    /// </summary>
    /// <returns>The double value.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when there's not enough data to read a double.</exception>
    public double ReadDouble()
    {
        if (Remaining < 8)
            throw new IndexOutOfRangeException("Not enough data to read double");

        double result = BitConverter.ToDouble(_data, _position);
        _position += 8;
        return result;
    }

    /// <summary>
    /// Recycles this PacketReader instance back to the pool for reuse.
    /// Clears the current data and resets position, then adds the instance to the pool if under maximum size.
    /// </summary>
    internal void Recycle()
    {
        _data = [];
        _position = 0;
        _end = 0;
        SubTag = 0;
        _pool.Release(this);
    }

    /// <summary>
    /// CLear all data in the PacketReader
    /// </summary>
    internal void Clear()
    {
        _data = [];
        _position = 0;
        _end = 0;
        SubTag = 0;
    }

    /// <inheritdoc/>
    public byte[] GetBytes()
    {
        return _data[_position..];
    }

    /// <inheritdoc/>
    public byte[] GetSubpacketBytes()
    {
        if (_end <= _position)
            return [];

        int length = _end - _position;
        byte[] result = new byte[length];
        Array.Copy(_data, _position, result, 0, length);
        return result;
    }
}
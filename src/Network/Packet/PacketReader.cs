using ReplantedOnline.Items.Enums;
using System.Text;

namespace ReplantedOnline.Network.Packet;

/// <summary>
/// Provides a pooled packet reader for efficient network packet parsing.
/// Handles reading various data types from a byte buffer with object pooling to reduce GC pressure.
/// </summary>
internal class PacketReader
{
    private byte[] _data = [];
    private int _position = 0;
    private static readonly Queue<PacketReader> _pool = [];
    private const int MAX_POOL_SIZE = 10;

    /// <summary>
    /// Retrieves a PacketReader instance from the pool or creates a new one, initialized with the provided data.
    /// </summary>
    /// <param name="data">The byte array containing packet data to read from.</param>
    /// <returns>A PacketReader instance ready for reading the provided data.</returns>
    internal static PacketReader Get(byte[] data)
    {
        PacketReader reader;

        reader = _pool.Count > 0 ? _pool.Dequeue() : new PacketReader();

        reader._data = data;
        reader._position = 0;
        return reader;
    }

    /// <summary>
    /// Reads the packet tag from the current position.
    /// </summary>
    /// <returns>The PacketTag identifying the packet type.</returns>
    internal PacketTag GetTag()
    {
        return (PacketTag)ReadByte();
    }

    /// <summary>
    /// Reads a string from the packet, expecting length-prefixed UTF-8 encoding.
    /// </summary>
    /// <returns>The decoded string value.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when there's not enough data to read the string.</exception>
    internal string ReadString()
    {
        int length = ReadInt();
        if (_position + length > _data.Length)
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
        if (_position + 4 > _data.Length)
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
        if (_position + 4 > _data.Length)
            throw new IndexOutOfRangeException("Not enough data to read uint");

        uint result = BitConverter.ToUInt32(_data, _position);
        _position += 4;
        return result;
    }

    /// <summary>
    /// Reads a 4-byte floating-point value from the packet.
    /// </summary>
    /// <returns>The float value.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when there's not enough data to read a float.</exception>
    internal float ReadFloat()
    {
        if (_position + 4 > _data.Length)
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
        if (_position >= _data.Length)
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
        if (_position >= _data.Length)
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
        int length = ReadInt();
        if (_position + length > _data.Length)
            throw new IndexOutOfRangeException("Not enough data to read bytes");

        byte[] result = new byte[length];
        Array.Copy(_data, _position, result, 0, length);
        _position += length;
        return result;
    }

    /// <summary>
    /// Reads an 8-byte signed integer from the packet.
    /// </summary>
    /// <returns>The long value.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when there's not enough data to read a long.</exception>
    internal long ReadLong()
    {
        if (_position + 8 > _data.Length)
            throw new IndexOutOfRangeException("Not enough data to read long");

        long result = BitConverter.ToInt64(_data, _position);
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
        if (_position + 8 > _data.Length)
            throw new IndexOutOfRangeException("Not enough data to read double");

        double result = BitConverter.ToDouble(_data, _position);
        _position += 8;
        return result;
    }

    /// <summary>
    /// Gets the number of bytes remaining to be read in the packet.
    /// </summary>
    internal int Remaining => _data.Length - _position;

    /// <summary>
    /// Recycles this PacketReader instance back to the pool for reuse.
    /// Clears the current data and resets position, then adds the instance to the pool if under maximum size.
    /// </summary>
    internal void Recycle()
    {
        _data = [];
        _position = 0;

        if (_pool.Count < MAX_POOL_SIZE)
            _pool.Enqueue(this);
    }
}
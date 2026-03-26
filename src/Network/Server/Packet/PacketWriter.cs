using Il2CppSteamworks;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client.Object;
using ReplantedOnline.Structs;
using System.Net;
using System.Text;
using UnityEngine;

namespace ReplantedOnline.Network.Server.Packet;

/// <summary>
/// Provides a pooled packet writer for efficient network packet construction.
/// Handles writing various data types to a byte buffer with object pooling to reduce GC pressure.
/// </summary>
internal sealed class PacketWriter : IPacket
{
    private List<byte> _data = [];
    private static readonly Queue<PacketWriter> _pool = [];
    private const int MAX_POOL_SIZE = 10;
    internal static int AmountInUse;

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
    /// Retrieves a PacketWriter instance from the pool or creates a new one, initialized with the provided data.
    /// </summary>
    /// <param name="data">The byte array containing initial packet data to write from.</param>
    /// <returns>A PacketWriter instance initialized with the specified data.</returns>
    internal static PacketWriter Get(byte[] data)
    {
        AmountInUse++;
        var writer = _pool.Count > 0 ? _pool.Dequeue() : new PacketWriter();
        writer._data = [.. data];
        return writer;
    }

    /// <summary>
    /// Writes an ID to the packet.
    /// </summary>
    /// <param name="id">The ID value to write.</param>
    internal void WriteID(ID id)
    {
        if (id == null || id.IsNull)
        {
            WriteByte(0); // None type
            return;
        }

        if (id.IsSteamId && id.TryGetSteamId(out SteamId steamId))
        {
            WriteByte(1); // SteamId type
            WriteULong(steamId);
        }
        else if (id.IsULong && id.TryGetULong(out ulong ulongValue))
        {
            WriteByte(2); // UInt type
            WriteULong(ulongValue);
        }
        else if (id.IsIPEndPoint && id.TryGetIPEndPoint(out IPEndPoint endpoint))
        {
            WriteByte(3); // IPEndPoint type
            WriteString(endpoint.Address.ToString());
            WriteInt(endpoint.Port);
        }
        else
        {
            WriteByte(0);
        }
    }

    /// <summary>
    /// Writes an networkclass.
    /// </summary>
    internal void WriteNetworkObject(NetworkObject networkObj)
    {
        if (networkObj != null)
        {
            WriteUInt(networkObj.NetworkId);
        }
        else
        {
            WriteUInt(NetworkObject.NULL);
        }
    }

    /// <summary>
    /// Writes another packet's contents into this packet writer.
    /// </summary>
    /// <param name="packet">The packet whose contents will be written.</param>
    internal void WritePacket(IPacket packet)
    {
        _data.AddRange(packet.GetByteBuffer());
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
    /// Writes an enum value to the packet as an integer.
    /// </summary>
    /// <typeparam name="T">The enum type to write</typeparam>
    /// <param name="value">The enum value to write</param>
    internal void WriteEnum<T>(T value) where T : Enum
    {
        WriteInt(Convert.ToInt32(value));
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

    /// <summary>
    /// Gets the complete packet data that has been written to the packet writer.
    /// </summary>
    /// <returns>
    /// A byte array containing all data written to the packet writer.
    /// </returns>
    public byte[] GetByteBuffer()
    {
        return [.. _data];
    }
}
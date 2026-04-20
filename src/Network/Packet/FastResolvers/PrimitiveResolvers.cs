using ReplantedOnline.Attributes;
using ReplantedOnline.Interfaces.Network;
using UnityEngine;

namespace ReplantedOnline.Network.Packet.FastResolvers;

[RegisterFastPacketResolver]
internal class IntResolver : IFastPacketResolver<int>
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type == typeof(int);

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter, int value) => packetWriter.WriteInt(value);

    /// <inheritdoc/>
    public int Deserialize(PacketReader packetReader, Type type) => packetReader.ReadInt();
}

[RegisterFastPacketResolver]
internal class UIntResolver : IFastPacketResolver<uint>
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type == typeof(uint);

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter, uint value) => packetWriter.WriteUInt(value);

    /// <inheritdoc/>
    public uint Deserialize(PacketReader packetReader, Type type) => packetReader.ReadUInt();
}

[RegisterFastPacketResolver]
internal class LongResolver : IFastPacketResolver<long>
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type == typeof(long);

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter, long value) => packetWriter.WriteLong(value);

    /// <inheritdoc/>
    public long Deserialize(PacketReader packetReader, Type type) => packetReader.ReadLong();
}

[RegisterFastPacketResolver]
internal class ULongResolver : IFastPacketResolver<ulong>
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type == typeof(ulong);

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter, ulong value) => packetWriter.WriteULong(value);

    /// <inheritdoc/>
    public ulong Deserialize(PacketReader packetReader, Type type) => packetReader.ReadULong();
}

[RegisterFastPacketResolver]
internal class ByteResolver : IFastPacketResolver<byte>
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type == typeof(byte);

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter, byte value) => packetWriter.WriteByte(value);

    /// <inheritdoc/>
    public byte Deserialize(PacketReader packetReader, Type type) => packetReader.ReadByte();
}

[RegisterFastPacketResolver]
internal class BoolResolver : IFastPacketResolver<bool>
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type == typeof(bool);

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter, bool value) => packetWriter.WriteBool(value);

    /// <inheritdoc/>
    public bool Deserialize(PacketReader packetReader, Type type) => packetReader.ReadBool();
}

[RegisterFastPacketResolver]
internal class FloatResolver : IFastPacketResolver<float>
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type == typeof(float);

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter, float value) => packetWriter.WriteFloat(value);

    /// <inheritdoc/>
    public float Deserialize(PacketReader packetReader, Type type) => packetReader.ReadFloat();
}

[RegisterFastPacketResolver]
internal class DoubleResolver : IFastPacketResolver<double>
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type == typeof(double);

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter, double value) => packetWriter.WriteDouble(value);

    /// <inheritdoc/>
    public double Deserialize(PacketReader packetReader, Type type) => packetReader.ReadDouble();
}

[RegisterFastPacketResolver]
internal class StringResolver : IFastPacketResolver<string>
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type == typeof(string);

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter, string value) => packetWriter.WriteString(value);

    /// <inheritdoc/>
    public string Deserialize(PacketReader packetReader, Type type) => packetReader.ReadString();
}

[RegisterFastPacketResolver]
internal class ByteArrayResolver : IFastPacketResolver<byte[]>
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type == typeof(byte[]);

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter, byte[] value) => packetWriter.WriteBytes(value);

    /// <inheritdoc/>
    public byte[] Deserialize(PacketReader packetReader, Type type) => packetReader.ReadBytes();
}

[RegisterFastPacketResolver]
internal class Vector2Resolver : IFastPacketResolver<Vector2>
{
    /// <inheritdoc/>
    public bool CanResolve(Type type) => type == typeof(Vector2);

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter, Vector2 value) => packetWriter.WriteVector2(value);

    /// <inheritdoc/>
    public Vector2 Deserialize(PacketReader packetReader, Type type) => packetReader.ReadVector2();
}
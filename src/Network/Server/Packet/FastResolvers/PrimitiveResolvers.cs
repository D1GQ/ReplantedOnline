using ReplantedOnline.Attributes;
using ReplantedOnline.Interfaces.Network;
using UnityEngine;

namespace ReplantedOnline.Network.Server.Packet.FastResolvers;

[RegisterFastPacketResolver]
internal class IntResolver : IFastPacketResolver<int>
{
    public bool CanResolve(Type type) => type == typeof(int);
    public void Serialize(PacketWriter packetWriter, int value) => packetWriter.WriteInt(value);
    public int Deserialize(PacketReader packetReader, Type type) => packetReader.ReadInt();
}

[RegisterFastPacketResolver]
internal class UIntResolver : IFastPacketResolver<uint>
{
    public bool CanResolve(Type type) => type == typeof(uint);
    public void Serialize(PacketWriter packetWriter, uint value) => packetWriter.WriteUInt(value);
    public uint Deserialize(PacketReader packetReader, Type type) => packetReader.ReadUInt();
}

[RegisterFastPacketResolver]
internal class LongResolver : IFastPacketResolver<long>
{
    public bool CanResolve(Type type) => type == typeof(long);
    public void Serialize(PacketWriter packetWriter, long value) => packetWriter.WriteLong(value);
    public long Deserialize(PacketReader packetReader, Type type) => packetReader.ReadLong();
}

[RegisterFastPacketResolver]
internal class ULongResolver : IFastPacketResolver<ulong>
{
    public bool CanResolve(Type type) => type == typeof(ulong);
    public void Serialize(PacketWriter packetWriter, ulong value) => packetWriter.WriteULong(value);
    public ulong Deserialize(PacketReader packetReader, Type type) => packetReader.ReadULong();
}

[RegisterFastPacketResolver]
internal class ByteResolver : IFastPacketResolver<byte>
{
    public bool CanResolve(Type type) => type == typeof(byte);
    public void Serialize(PacketWriter packetWriter, byte value) => packetWriter.WriteByte(value);
    public byte Deserialize(PacketReader packetReader, Type type) => packetReader.ReadByte();
}

[RegisterFastPacketResolver]
internal class BoolResolver : IFastPacketResolver<bool>
{
    public bool CanResolve(Type type) => type == typeof(bool);
    public void Serialize(PacketWriter packetWriter, bool value) => packetWriter.WriteBool(value);
    public bool Deserialize(PacketReader packetReader, Type type) => packetReader.ReadBool();
}

[RegisterFastPacketResolver]
internal class FloatResolver : IFastPacketResolver<float>
{
    public bool CanResolve(Type type) => type == typeof(float);
    public void Serialize(PacketWriter packetWriter, float value) => packetWriter.WriteFloat(value);
    public float Deserialize(PacketReader packetReader, Type type) => packetReader.ReadFloat();
}

[RegisterFastPacketResolver]
internal class DoubleResolver : IFastPacketResolver<double>
{
    public bool CanResolve(Type type) => type == typeof(double);
    public void Serialize(PacketWriter packetWriter, double value) => packetWriter.WriteDouble(value);
    public double Deserialize(PacketReader packetReader, Type type) => packetReader.ReadDouble();
}

[RegisterFastPacketResolver]
internal class StringResolver : IFastPacketResolver<string>
{
    public bool CanResolve(Type type) => type == typeof(string);
    public void Serialize(PacketWriter packetWriter, string value) => packetWriter.WriteString(value);
    public string Deserialize(PacketReader packetReader, Type type) => packetReader.ReadString();
}

[RegisterFastPacketResolver]
internal class ByteArrayResolver : IFastPacketResolver<byte[]>
{
    public bool CanResolve(Type type) => type == typeof(byte[]);
    public void Serialize(PacketWriter packetWriter, byte[] value) => packetWriter.WriteBytes(value);
    public byte[] Deserialize(PacketReader packetReader, Type type) => packetReader.ReadBytes();
}

[RegisterFastPacketResolver]
internal class Vector2Resolver : IFastPacketResolver<Vector2>
{
    public bool CanResolve(Type type) => type == typeof(Vector2);
    public void Serialize(PacketWriter packetWriter, Vector2 value) => packetWriter.WriteVector2(value);
    public Vector2 Deserialize(PacketReader packetReader, Type type) => packetReader.ReadVector2();
}
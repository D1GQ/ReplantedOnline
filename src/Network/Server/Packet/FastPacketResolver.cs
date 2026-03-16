using ReplantedOnline.Network.Client.Object;
using ReplantedOnline.Structs;
using UnityEngine;

namespace ReplantedOnline.Network.Server.Packet;

/// <summary>
/// Provides fast packet serialization and deserialization methods with type checking support.
/// </summary>
internal static class FastPacketResolver
{
    /// <summary>
    /// Checks if a type is supported by the FastResolverResolver read/write methods.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is supported, false otherwise.</returns>
    internal static bool IsTypeSupported(Type type)
    {
        if (type == typeof(int) ||
            type == typeof(uint) ||
            type == typeof(long) ||
            type == typeof(ulong) ||
            type == typeof(short) ||
            type == typeof(ushort) ||
            type == typeof(byte) ||
            type == typeof(bool) ||
            type == typeof(float) ||
            type == typeof(double) ||
            type == typeof(string) ||
            type == typeof(Vector2) ||
            type == typeof(ID) ||
            type == typeof(NetworkObject) ||
            type == typeof(byte[]) ||
            type == typeof(PacketWriter) ||
            type.IsEnum)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Automatically writes any supported type to the packet without adding type information.
    /// Supports: primitive types, string, Vector2, ID, NetworkObject, byte[], PacketWriter, and XmlSum.
    /// </summary>
    /// <typeparam name="T">The type of value to write. Must be a supported type.</typeparam>
    /// <param name="writer">The packet writer to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <exception cref="NotSupportedException">Thrown when the type T is not supported.</exception>
    internal static void WriteFast<T>(PacketWriter writer, T value)
    {
        if (value == null)
        {
            return;
        }

        switch (value)
        {
            case int intVal:
                writer.WriteInt(intVal);
                break;
            case uint uintVal:
                writer.WriteUInt(uintVal);
                break;
            case long longVal:
                writer.WriteLong(longVal);
                break;
            case ulong ulongVal:
                writer.WriteULong(ulongVal);
                break;
            case byte byteVal:
                writer.WriteByte(byteVal);
                break;
            case bool boolVal:
                writer.WriteBool(boolVal);
                break;
            case float floatVal:
                writer.WriteFloat(floatVal);
                break;
            case double doubleVal:
                writer.WriteDouble(doubleVal);
                break;
            case string stringVal:
                writer.WriteString(stringVal);
                break;
            case Vector2 vectorVal:
                writer.WriteVector2(vectorVal);
                break;
            case ID idVal:
                writer.WriteID(idVal);
                break;
            case NetworkObject netObjVal:
                writer.WriteNetworkObject(netObjVal);
                break;
            case byte[] byteArrayVal:
                writer.WriteBytes(byteArrayVal);
                break;
            case PacketWriter packetVal:
                writer.WritePacket(packetVal);
                break;
            case Enum enumVal:
                writer.WriteInt(Convert.ToInt32(enumVal));
                break;
            default:
                throw new NotSupportedException($"Type {typeof(T)} is not supported by WriteFast");
        }
    }

    /// <summary>
    /// Generic version of ReadFast for compile-time type safety.
    /// </summary>
    /// <typeparam name="T">The type of value to read. Must be a supported type.</typeparam>
    /// <param name="reader">The packet reader to read from.</param>
    /// <returns>The read value.</returns>
    /// <exception cref="NotSupportedException">Thrown when the type T is not supported.</exception>
    internal static T ReadFast<T>(PacketReader reader)
    {
        return (T)ReadFast(reader, typeof(T));
    }

    /// <summary>
    /// Automatically reads a value of the specified type from the packet without expecting type information.
    /// </summary>
    /// <param name="reader">The packet reader to read from.</param>
    /// <param name="type">The type of value to read. Must be a supported type.</param>
    /// <returns>The read value as an object.</returns>
    /// <exception cref="NotSupportedException">Thrown when the type is not supported.</exception>
    internal static object ReadFast(PacketReader reader, Type type)
    {
        if (type == typeof(int))
            return reader.ReadInt();

        if (type == typeof(uint))
            return reader.ReadUInt();

        if (type == typeof(long))
            return reader.ReadLong();

        if (type == typeof(ulong))
            return reader.ReadULong();

        if (type == typeof(byte))
            return reader.ReadByte();

        if (type == typeof(bool))
            return reader.ReadBool();

        if (type == typeof(float))
            return reader.ReadFloat();

        if (type == typeof(double))
            return reader.ReadDouble();

        if (type == typeof(string))
            return reader.ReadString();

        if (type == typeof(Vector2))
            return reader.ReadVector2();

        if (type == typeof(ID))
            return reader.ReadID();

        if (type == typeof(NetworkObject))
            return reader.ReadNetworkObject();

        if (type == typeof(byte[]))
            return reader.ReadBytes();

        if (type.IsEnum)
        {
            int enumValue = reader.ReadInt();
            return Enum.ToObject(type, enumValue);
        }

        Type underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            throw new NotSupportedException($"Nullable types require explicit null handling. Use a non-nullable type or add null checks manually.");
        }

        throw new NotSupportedException($"Type {type} is not supported by ReadFast");
    }
}
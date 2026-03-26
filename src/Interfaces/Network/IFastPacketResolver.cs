using ReplantedOnline.Attributes;
using ReplantedOnline.Network.Server.Packet;

namespace ReplantedOnline.Interfaces.Network;

/// <summary>
/// Provides non-generic packet serialization and deserialization capabilities.
/// Includes static helper methods for automatic resolver discovery and caching.
/// </summary>
internal interface IFastPacketResolver
{
    /// <summary>
    /// Determines whether this resolver can handle the specified type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if this resolver can handle the type; otherwise, false.</returns>
    bool CanResolve(Type type);

    /// <summary>
    /// Serializes an object to the packet writer without type safety.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write to.</param>
    /// <param name="value">The object to serialize.</param>
    void UnsafeSerialize(PacketWriter packetWriter, object value);

    /// <summary>
    /// Deserializes an object from the packet reader without type safety.
    /// </summary>
    /// <param name="packetReader">The packet reader to read from.</param>
    /// <param name="type">The type.</param>
    /// <returns>The deserialized object.</returns>
    object UnsafeDeserialize(PacketReader packetReader, Type type);

    /// <summary>
    /// Static cache of resolved type-to-resolver mappings for performance optimization.
    /// </summary>
    private readonly static Dictionary<Type, IFastPacketResolver> _resolverLookup = [];

    /// <summary>
    /// Generic version of WriteFast for compile-time type safety.
    /// Automatically writes any supported type to the packet without adding type information.
    /// </summary>
    /// <typeparam name="T">The type of value to write. Must be a supported type.</typeparam>
    /// <param name="packetWriter">The packet writer to write to.</param>
    /// <param name="value">The value to write. Can be null for reference types.</param>
    /// <exception cref="NotSupportedException">Thrown when the type T is not supported by any registered resolver.</exception>
    internal static void WriteFast<T>(PacketWriter packetWriter, T value)
    {
        WriteFast(packetWriter, value, typeof(T));
    }

    /// <summary>
    /// Automatically writes any supported type to the packet without adding type information.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write to.</param>
    /// <param name="value">The value to write. Can be null for reference types.</param>
    /// <param name="type">The type of the value to write.</param>
    /// <exception cref="NotSupportedException">Thrown when the type T is not supported by any registered resolver.</exception>
    internal static void WriteFast(PacketWriter packetWriter, object value, Type type)
    {
        if (!_resolverLookup.TryGetValue(type, out var resolver))
        {
            foreach (var fastResolver in RegisterFastPacketResolver.Instances)
            {
                if (fastResolver.CanResolve(type))
                {
                    _resolverLookup[type] = resolver = fastResolver;
                    break;
                }
            }
        }

        if (resolver != null)
        {
            resolver.UnsafeSerialize(packetWriter, value);
            return;
        }

        throw new NotSupportedException($"Type {type} is not supported by IFastPacketResolver");
    }

    /// <summary>
    /// Generic version of ReadFast for compile-time type safety.
    /// Automatically discovers and uses the appropriate resolver for the specified type.
    /// </summary>
    /// <typeparam name="T">The type of value to read. Must be a supported type.</typeparam>
    /// <param name="packetReader">The packet reader to read from.</param>
    /// <returns>The read value cast to the specified type.</returns>
    /// <exception cref="NotSupportedException">Thrown when the type T is not supported by any registered resolver.</exception>
    internal static T ReadFast<T>(PacketReader packetReader)
    {
        return (T)ReadFast(packetReader, typeof(T));
    }

    /// <summary>
    /// Automatically reads a value of the specified type from the packet without expecting type information.
    /// Uses a cached lookup for performance and automatically discovers the appropriate resolver.
    /// </summary>
    /// <param name="packetReader">The packet reader to read from.</param>
    /// <param name="type">The type of value to read. Must be a supported type.</param>
    /// <returns>The deserialized value as an object.</returns>
    /// <exception cref="NotSupportedException">Thrown when the type is not supported by any registered resolver.</exception>
    internal static object ReadFast(PacketReader packetReader, Type type)
    {
        if (!_resolverLookup.TryGetValue(type, out var resolver))
        {
            foreach (var fastResolver in RegisterFastPacketResolver.Instances)
            {
                if (fastResolver.CanResolve(type))
                {
                    _resolverLookup[type] = resolver = fastResolver;
                    break;
                }
            }
        }

        if (resolver != null)
        {
            return resolver.UnsafeDeserialize(packetReader, type);
        }

        throw new NotSupportedException($"Type {type} is not supported by IFastPacketResolver");
    }
}

/// <summary>
/// Provides type-safe packet serialization and deserialization capabilities for a specific type.
/// Implements the non-generic IFastPacketResolver interface with type-safe methods.
/// </summary>
/// <typeparam name="T">The type that this resolver handles.</typeparam>
internal interface IFastPacketResolver<T> : IFastPacketResolver
{
    /// <summary>
    /// Serializes a strongly-typed value to the packet writer.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write to.</param>
    /// <param name="value">The value to serialize. Can be null for reference types.</param>
    void Serialize(PacketWriter packetWriter, T value);

    /// <summary>
    /// Deserializes a strongly-typed value from the packet reader.
    /// </summary>
    /// <param name="packetReader">The packet reader to read from.</param>
    /// <param name="type">The type.</param>
    /// <returns>The deserialized value of type T.</returns>
    T Deserialize(PacketReader packetReader, Type type);

    /// <inheritdoc />
    /// <remarks>
    /// This method provides a non-generic wrapper around the type-safe Serialize method.
    /// It casts the object parameter to type T before serialization.
    /// </remarks>
    void IFastPacketResolver.UnsafeSerialize(PacketWriter packetWriter, object value)
    {
        Serialize(packetWriter, (T)value);
    }

    /// <remarks>
    /// This method provides a non-generic wrapper around the type-safe Deserialize method.
    /// It returns the deserialized value as an object.
    /// </remarks>
    object IFastPacketResolver.UnsafeDeserialize(PacketReader packetReader, Type type)
    {
        return Deserialize(packetReader, type);
    }
}
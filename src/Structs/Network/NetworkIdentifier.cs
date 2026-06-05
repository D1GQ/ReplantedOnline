using ReplantedOnline.Network.Client;

namespace ReplantedOnline.Structs.Network;

/// <summary>
/// Represents a unique network identifier.
/// Combines a client index and a local ID into a single 32-bit value.
/// </summary>
internal readonly struct NetworkIdentifier : IEquatable<NetworkIdentifier>
{
    /// <summary>
    /// The maximum value that can be stored in the local ID portion (24 bits).
    /// </summary>
    internal const uint MAX_LOCAL_ID = 0xFFFFFF;

    private NetworkIdentifier(byte clientIndex, uint localId)
    {
        ClientIndex = clientIndex;
        LocalId = localId;
    }

    /// <summary>
    /// Gets a null/empty network identifier with default values.
    /// </summary>
    internal static NetworkIdentifier Null => default;

    /// <summary>
    /// Gets the index of the client that owns this identifier.
    /// </summary>
    internal byte ClientIndex { get; }

    /// <summary>
    /// Gets the local identifier unique to the owning client.
    /// </summary>
    internal uint LocalId { get; }

    /// <summary>
    /// Gets the combined 32-bit identifier value.
    /// </summary>
    internal uint Id => ((uint)ClientIndex << 24) | LocalId;

    /// <summary>
    /// Creates a new network identifier for the local client.
    /// </summary>
    /// <param name="localId">The local identifier value. Must be between 0 and MAX_LOCAL_ID (16,777,215).</param>
    /// <returns>A new <see cref="NetworkIdentifier"/> for the local client.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="localId"/> exceeds MAX_LOCAL_ID.</exception>
    internal static NetworkIdentifier Create(uint localId)
    {
        if (localId > MAX_LOCAL_ID)
        {
            throw new ArgumentOutOfRangeException(nameof(localId));
        }

        return new NetworkIdentifier(ReloadedClientData.LocalClient!.GetClientIndex(), localId);
    }

    /// <summary>
    /// Reconstructs a network identifier from a combined 32-bit ID value.
    /// </summary>
    /// <param name="id">The combined identifier value (8-bit client index + 24-bit local ID).</param>
    /// <returns>A <see cref="NetworkIdentifier"/> representing the provided ID.</returns>
    internal static NetworkIdentifier Get(uint id)
    {
        byte clientIndex = (byte)(id >> 24);
        uint localId = id & MAX_LOCAL_ID;

        return new NetworkIdentifier(clientIndex, localId);
    }

    /// <summary>
    /// Returns the next sequential network identifier for the same client.
    /// </summary>
    /// <returns>A new <see cref="NetworkIdentifier"/> with <see cref="LocalId"/> incremented by 1.</returns>
    internal NetworkIdentifier Next()
    {
        return new NetworkIdentifier(ClientIndex, LocalId + 1);
    }

    /// <inheritdoc />
    public bool Equals(NetworkIdentifier other) => Id == other.Id;

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is NetworkIdentifier other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode() => (int)Id;

    /// <summary>
    /// Determines whether two specified network identifiers have the same value.
    /// </summary>
    /// <param name="left">The first identifier to compare.</param>
    /// <param name="right">The second identifier to compare.</param>
    /// <returns><c>true</c> if the identifiers are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(NetworkIdentifier left, NetworkIdentifier right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two specified network identifiers have different values.
    /// </summary>
    /// <param name="left">The first identifier to compare.</param>
    /// <param name="right">The second identifier to compare.</param>
    /// <returns><c>true</c> if the identifiers are different; otherwise, <c>false</c>.</returns>
    public static bool operator !=(NetworkIdentifier left, NetworkIdentifier right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{ClientIndex}.{LocalId}";
    }
}
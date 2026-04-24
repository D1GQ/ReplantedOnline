using Il2CppSteamworks;
using ReplantedOnline.Data.Json;
using ReplantedOnline.Enums;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace ReplantedOnline.Structs;

/// <summary>
/// Unified identifier supporting SteamId (Steam), ulong (simplified IDs), and IPEndPoint (LAN).
/// Provides type-safe storage and comparison between different ID types.
/// </summary>
[JsonConverter(typeof(IDJsonConverter))]
internal readonly struct ID : IEquatable<ID>, IComparable<ID>
{
    private readonly object _id;
    private readonly IdType _type;

    /// <summary>
    /// Gets a null/empty ID instance.
    /// </summary>
    internal static ID Null { get; } = new ID(null, IdType.Null);

    /// <summary>
    /// Initializes a new instance of the ID struct with the specified value and type.
    /// </summary>
    /// <param name="id">The underlying ID value.</param>
    /// <param name="type">The type of ID being stored.</param>
    internal ID(object id, IdType type)
    {
        _id = id;
        _type = type;
    }

    /// <summary>
    /// Creates a new ID instance with a randomly generated 64-bit unsigned integer value.
    /// </summary>
    /// <returns></returns>
    internal static ID CreateRandomULong()
    {
        ulong randomId;
        using (var rng = RandomNumberGenerator.Create())
        {
            var bytes = new byte[8];
            rng.GetBytes(bytes);
            randomId = BitConverter.ToUInt64(bytes, 0);
        }
        return new ID(randomId, IdType.ULong);
    }

    /// <summary>
    /// Gets a value indicating whether this ID represents a null/empty value.
    /// </summary>
    internal bool IsNull => _type == IdType.Null;

    /// <summary>
    /// Gets a value indicating whether this ID represents a Steam ID.
    /// </summary>
    internal bool IsSteamId => _type == IdType.SteamId;

    /// <summary>
    /// Gets a value indicating whether this ID represents a 64-bit unsigned integer ID.
    /// </summary>
    internal bool IsULong => _type == IdType.ULong;

    /// <summary>
    /// Gets a value indicating whether this ID represents an IP endpoint (LAN).
    /// </summary>
    internal bool IsIPEndPoint => _type == IdType.IPEndPoint;

    /// <summary>
    /// Gets a value indicating whether this ID has a non-null value.
    /// </summary>
    internal bool HasValue => _type != IdType.Null;

    /// <summary>
    /// Attempts to retrieve the ID as a Steam ID.
    /// </summary>
    /// <param name="steamId">When this method returns, contains the Steam ID if successful; otherwise, default.</param>
    /// <returns>true if the ID is a Steam ID; otherwise, false.</returns>
    internal bool TryGetSteamId(out SteamId steamId)
    {
        if (_type == IdType.SteamId && _id != null)
        {
            steamId = (SteamId)_id;
            return true;
        }

        if (_type == IdType.ULong && _id != null)
        {
            steamId = (ulong)_id;
            return true;
        }

        steamId = default;
        return false;
    }

    /// <summary>
    /// Attempts to retrieve the ID as a 64-bit unsigned integer.
    /// </summary>
    /// <param name="id">When this method returns, contains the ulong ID if successful; otherwise, 0.</param>
    /// <returns>true if the ID is a ulong; otherwise, false.</returns>
    internal bool TryGetULong(out ulong id)
    {
        if (_type == IdType.ULong && _id != null)
        {
            id = (ulong)_id;
            return true;
        }

        if (_type == IdType.SteamId && _id != null)
        {
            id = (SteamId)_id;
            return true;
        }

        id = 0;
        return false;
    }

    /// <summary>
    /// Attempts to retrieve the ID as an IP endpoint.
    /// </summary>
    /// <param name="endpoint">When this method returns, contains the IP endpoint if successful; otherwise, default.</param>
    /// <returns>true if the ID is an IP endpoint; otherwise, false.</returns>
    internal bool TryGetIPEndPoint(out IPEndPoint endpoint)
    {
        if (_type == IdType.IPEndPoint && _id != null)
        {
            endpoint = (IPEndPoint)_id;
            return true;
        }
        endpoint = default;
        return false;
    }

    /// <summary>
    /// Returns the ID as a Steam ID.
    /// </summary>
    /// <returns>The Steam ID value.</returns>
    /// <exception cref="InvalidCastException">Thrown when the ID cannot be converted to a Steam ID.</exception>
    internal SteamId AsSteamId()
    {
        if (TryGetSteamId(out var id))
            return id;
        throw new InvalidCastException($"ID cannot be converted to SteamId (current type: {_type})");
    }

    /// <summary>
    /// Returns the ID as a 64-bit unsigned integer.
    /// </summary>
    /// <returns>The ulong value.</returns>
    /// <exception cref="InvalidCastException">Thrown when the ID cannot be converted to a ulong.</exception>
    internal ulong AsULong()
    {
        if (TryGetULong(out var id))
            return id;
        throw new InvalidCastException($"ID cannot be converted to ulong (current type: {_type})");
    }

    /// <summary>
    /// Returns the ID as an IP endpoint.
    /// </summary>
    /// <returns>The IP endpoint value.</returns>
    /// <exception cref="InvalidCastException">Thrown when the ID is not an IP endpoint.</exception>
    internal IPEndPoint AsIPEndPoint()
    {
        if (TryGetIPEndPoint(out var endpoint))
            return endpoint;
        throw new InvalidCastException($"ID is not an IPEndPoint (current type: {_type})");
    }

    /// <summary>
    /// Determines whether two ID instances are equal.
    /// </summary>
    public static bool operator ==(ID left, ID right) => left.Equals(right);

    /// <summary>
    /// Determines whether two ID instances are not equal.
    /// </summary>
    public static bool operator !=(ID left, ID right) => !left.Equals(right);

    // ===== IMPLICIT CONVERSIONS =====

    /// <summary>
    /// Implicitly converts a SteamId to an ID (stores as SteamId type).
    /// </summary>
    public static implicit operator ID(SteamId steamId) => new(steamId, IdType.SteamId);

    /// <summary>
    /// Implicitly converts a ulong to an ID (stores as ULong type).
    /// </summary>
    public static implicit operator ID(ulong id) => new(id, IdType.ULong);

    /// <summary>
    /// Implicitly converts an IPEndPoint to an ID (stores as IPEndPoint type).
    /// </summary>
    public static implicit operator ID(IPEndPoint endpoint) => new(endpoint, IdType.IPEndPoint);

    // ===== EXPLICIT CONVERSIONS =====

    /// <summary>
    /// Explicitly converts an ID to a SteamId.
    /// Works for both SteamId and ULong types (since ulong implicitly converts to SteamId).
    /// </summary>
    /// <exception cref="InvalidCastException">Thrown when the ID is neither SteamId nor ULong.</exception>
    public static explicit operator SteamId(ID id)
    {
        if (id.TryGetSteamId(out var steamId))
            return steamId;
        if (id.TryGetULong(out var ulongValue))
            return ulongValue; // implicit conversion from ulong to SteamId
        throw new InvalidCastException($"Cannot convert ID of type {id._type} to SteamId");
    }

    /// <summary>
    /// Explicitly converts an ID to a ulong.
    /// Works for both ULong and SteamId types (since SteamId implicitly converts to ulong).
    /// </summary>
    /// <exception cref="InvalidCastException">Thrown when the ID is neither ULong nor SteamId.</exception>
    public static explicit operator ulong(ID id)
    {
        if (id.TryGetULong(out var ulongValue))
            return ulongValue;
        if (id.TryGetSteamId(out var steamId))
            return steamId; // implicit conversion from SteamId to ulong
        throw new InvalidCastException($"Cannot convert ID of type {id._type} to ulong");
    }

    /// <summary>
    /// Explicitly converts an ID to an IPEndPoint.
    /// </summary>
    /// <exception cref="InvalidCastException">Thrown when the ID is not an IPEndPoint.</exception>
    public static explicit operator IPEndPoint(ID id) => id.AsIPEndPoint();

    // ===== EQUALITY MEMBERS =====

    /// <summary>
    /// Determines whether the specified object is equal to the current ID.
    /// </summary>
    public override bool Equals(object obj) => obj is ID other && Equals(other);

    /// <summary>
    /// Indicates whether the current ID is equal to another ID.
    /// </summary>
    public bool Equals(ID other)
    {
        if (_type == IdType.Null && other._type == IdType.Null)
            return true;

        if (_type == IdType.Null || other._type == IdType.Null)
            return false;

        // Same type comparison
        if (_type == other._type)
        {
            return _type switch
            {
                IdType.SteamId => ((SteamId)_id).Equals((SteamId)other._id),
                IdType.ULong => (ulong)_id == (ulong)other._id,
                IdType.IPEndPoint => ((IPEndPoint)_id).Equals((IPEndPoint)other._id),
                _ => false
            };
        }

        // Cross-type comparisons (SteamId <-> ULong)
        if (_type == IdType.SteamId && other._type == IdType.ULong)
        {
            return ((SteamId)_id).Value == (ulong)other._id;
        }
        if (_type == IdType.ULong && other._type == IdType.SteamId)
        {
            return (ulong)_id == ((SteamId)other._id).Value;
        }

        return false;
    }

    /// <summary>
    /// Compares the current ID with another ID and returns an integer that indicates 
    /// whether the current instance precedes, follows, or occurs in the same position 
    /// in the sort order as the other ID.
    /// </summary>
    public int CompareTo(ID other)
    {
        if (_type == IdType.Null && other._type == IdType.Null)
            return 0;
        if (_type == IdType.Null)
            return -1;
        if (other._type == IdType.Null)
            return 1;

        // Same type comparison
        if (_type == other._type)
        {
            return _type switch
            {
                IdType.SteamId => ((SteamId)_id).Value.CompareTo(((SteamId)other._id).Value),
                IdType.ULong => ((ulong)_id).CompareTo((ulong)other._id),
                IdType.IPEndPoint => CompareIPEndPoints((IPEndPoint)_id, (IPEndPoint)other._id),
                _ => 0
            };
        }

        // Cross-type comparisons (SteamId <-> ULong)
        if (_type == IdType.SteamId && other._type == IdType.ULong)
        {
            return ((SteamId)_id).Value.CompareTo((ulong)other._id);
        }
        if (_type == IdType.ULong && other._type == IdType.SteamId)
        {
            return ((ulong)_id).CompareTo(((SteamId)other._id).Value);
        }

        return ToString().CompareTo(other.ToString());
    }

    /// <summary>
    /// Compares two IP endpoints for sorting purposes.
    /// </summary>
    private static int CompareIPEndPoints(IPEndPoint a, IPEndPoint b)
    {
        byte[] aBytes = a.Address.GetAddressBytes();
        byte[] bBytes = b.Address.GetAddressBytes();

        for (int i = 0; i < Math.Min(aBytes.Length, bBytes.Length); i++)
        {
            if (aBytes[i] != bBytes[i])
                return aBytes[i].CompareTo(bBytes[i]);
        }

        return a.Port.CompareTo(b.Port);
    }

    /// <summary>
    /// Returns a hash code for this ID.
    /// </summary>
    public override int GetHashCode()
    {
        if (_type == IdType.Null)
            return 0;

        return _type switch
        {
            IdType.SteamId => ((SteamId)_id).Value.GetHashCode(),
            IdType.ULong => ((ulong)_id).GetHashCode(),
            IdType.IPEndPoint => ((IPEndPoint)_id).GetHashCode(),
            _ => 0
        };
    }

    /// <summary>
    /// Returns a string representation of the ID.
    /// </summary>
    public override string ToString()
    {
        if (_type == IdType.Null)
            return "Null";

        return _type switch
        {
            IdType.SteamId => $"Steam:{((SteamId)_id).Value}",
            IdType.ULong => $"ULong:{_id}",
            IdType.IPEndPoint => $"IPH:{GetIpHash((IPEndPoint)_id)}",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Generates a stable uint hash from an IPEndPoint.
    /// </summary>
    private static uint GetIpHash(IPEndPoint endPoint)
    {
        uint ipHash = (uint)endPoint.Address.GetHashCode();
        uint portHash = (uint)endPoint.Port;
        return (ipHash * 397) ^ portHash;
    }

    /// <summary>
    /// Gets the underlying type of the stored ID.
    /// </summary>
    public Type UnderlyingType => _type switch
    {
        IdType.SteamId => typeof(SteamId),
        IdType.ULong => typeof(ulong),
        IdType.IPEndPoint => typeof(IPEndPoint),
        _ => typeof(object)
    };

    /// <summary>
    /// Gets the underlying value of the stored ID.
    /// </summary>
    public object UnderlyingValue => _id;
}
using Il2CppSteamworks;
using ReplantedOnline.Enums;
using System.Net;

namespace ReplantedOnline.Structs;

/// <summary>
/// Unified identifier supporting SteamId (Steam), uint (simplified IDs), and IPEndPoint (LAN).
/// Provides type-safe storage and comparison between different ID types.
/// </summary>
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
    /// Gets a value indicating whether this ID represents a null/empty value.
    /// </summary>
    internal bool IsNull => _type == IdType.Null;

    /// <summary>
    /// Gets a value indicating whether this ID represents a Steam ID.
    /// </summary>
    internal bool IsSteamId => _type == IdType.SteamId;

    /// <summary>
    /// Gets a value indicating whether this ID represents a 32-bit unsigned integer ID.
    /// </summary>
    internal bool IsUInt => _type == IdType.UInt;

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
        steamId = default;
        return false;
    }

    /// <summary>
    /// Attempts to retrieve the ID as a 32-bit unsigned integer.
    /// </summary>
    /// <param name="id">When this method returns, contains the uint ID if successful; otherwise, 0.</param>
    /// <returns>true if the ID is a uint; otherwise, false.</returns>
    internal bool TryGetUInt(out uint id)
    {
        if (_type == IdType.UInt && _id != null)
        {
            id = (uint)_id;
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
    /// <exception cref="InvalidCastException">Thrown when the ID is not a Steam ID.</exception>
    internal SteamId AsSteamId()
    {
        if (TryGetSteamId(out var id))
            return id;
        throw new InvalidCastException($"ID is not a SteamId (current type: {_type})");
    }

    /// <summary>
    /// Returns the ID as a 32-bit unsigned integer.
    /// </summary>
    /// <returns>The uint value.</returns>
    /// <exception cref="InvalidCastException">Thrown when the ID is not a uint.</exception>
    internal uint AsUInt()
    {
        if (TryGetUInt(out var id))
            return id;
        throw new InvalidCastException($"ID is not a uint (current type: {_type})");
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

    /// <summary>
    /// Implicitly converts a SteamId to an ID.
    /// </summary>
    /// <param name="steamId">The Steam ID to convert.</param>
    public static implicit operator ID(SteamId steamId) => new(steamId, IdType.SteamId);

    /// <summary>
    /// Implicitly converts a uint to an ID.
    /// </summary>
    /// <param name="id">The uint to convert.</param>
    public static implicit operator ID(uint id) => new(id, IdType.UInt);

    /// <summary>
    /// Implicitly converts an IPEndPoint to an ID.
    /// </summary>
    /// <param name="endpoint">The IP endpoint to convert.</param>
    public static implicit operator ID(IPEndPoint endpoint) => new(endpoint, IdType.IPEndPoint);

    /// <summary>
    /// Explicitly converts an ID to a SteamId.
    /// </summary>
    /// <param name="id">The ID to convert.</param>
    /// <exception cref="InvalidCastException">Thrown when the ID is not a Steam ID.</exception>
    public static explicit operator SteamId(ID id) => id.AsSteamId();

    /// <summary>
    /// Explicitly converts an ID to a uint.
    /// </summary>
    /// <param name="id">The ID to convert.</param>
    /// <exception cref="InvalidCastException">Thrown when the ID is not a uint.</exception>
    public static explicit operator uint(ID id) => id.AsUInt();

    /// <summary>
    /// Explicitly converts an ID to an IPEndPoint.
    /// </summary>
    /// <param name="id">The ID to convert.</param>
    /// <exception cref="InvalidCastException">Thrown when the ID is not an IP endpoint.</exception>
    public static explicit operator IPEndPoint(ID id) => id.AsIPEndPoint();

    /// <summary>
    /// Determines whether the specified object is equal to the current ID.
    /// </summary>
    /// <param name="obj">The object to compare with the current ID.</param>
    /// <returns>true if the specified object is equal to the current ID; otherwise, false.</returns>
    public override bool Equals(object obj) => obj is ID other && Equals(other);

    /// <summary>
    /// Indicates whether the current ID is equal to another ID.
    /// </summary>
    /// <param name="other">An ID to compare with this ID.</param>
    /// <returns>true if the current ID is equal to the other ID; otherwise, false.</returns>
    public bool Equals(ID other)
    {
        if (_type == IdType.Null && other._type == IdType.Null)
            return true;

        if (_type == IdType.Null || other._type == IdType.Null)
            return false;

        if (_type == other._type)
        {
            return _type switch
            {
                IdType.SteamId => ((SteamId)_id).Equals((SteamId)other._id),
                IdType.UInt => (uint)_id == (uint)other._id,
                IdType.IPEndPoint => ((IPEndPoint)_id).Equals((IPEndPoint)other._id),
                _ => false
            };
        }

        if (_type == IdType.SteamId && other._type == IdType.UInt)
        {
            return ((SteamId)_id).AccountId == (ulong)(uint)other._id;
        }
        if (_type == IdType.UInt && other._type == IdType.SteamId)
        {
            return (ulong)(uint)_id == ((SteamId)other._id).AccountId;
        }

        return false;
    }

    /// <summary>
    /// Compares the current ID with another ID and returns an integer that indicates 
    /// whether the current instance precedes, follows, or occurs in the same position 
    /// in the sort order as the other ID.
    /// </summary>
    /// <param name="other">An ID to compare with this ID.</param>
    /// <returns>A value that indicates the relative order of the IDs being compared.</returns>
    public int CompareTo(ID other)
    {
        if (_type == IdType.Null && other._type == IdType.Null)
            return 0;
        if (_type == IdType.Null)
            return -1;
        if (other._type == IdType.Null)
            return 1;

        if (_type == other._type)
        {
            return _type switch
            {
                IdType.SteamId => ((SteamId)_id).AccountId.CompareTo(((SteamId)other._id).AccountId),
                IdType.UInt => ((uint)_id).CompareTo((uint)other._id),
                IdType.IPEndPoint => CompareIPEndPoints((IPEndPoint)_id, (IPEndPoint)other._id),
                _ => 0
            };
        }

        if (_type == IdType.SteamId && other._type == IdType.UInt)
        {
            return ((SteamId)_id).AccountId.CompareTo((ulong)(uint)other._id);
        }
        if (_type == IdType.UInt && other._type == IdType.SteamId)
        {
            return ((ulong)(uint)_id).CompareTo(((SteamId)other._id).AccountId);
        }

        return ToString().CompareTo(other.ToString());
    }

    /// <summary>
    /// Compares two IP endpoints for sorting purposes.
    /// </summary>
    /// <param name="a">The first IP endpoint.</param>
    /// <param name="b">The second IP endpoint.</param>
    /// <returns>A value indicating the relative order of the endpoints.</returns>
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
    /// <returns>A hash code for the current ID.</returns>
    public override int GetHashCode()
    {
        if (_type == IdType.Null)
            return 0;

        return _type switch
        {
            IdType.SteamId => ((SteamId)_id).AccountId.GetHashCode(),
            IdType.UInt => ((uint)_id).GetHashCode(),
            IdType.IPEndPoint => ((IPEndPoint)_id).GetHashCode(),
            _ => 0
        };
    }

    /// <summary>
    /// Returns a string representation of the ID.
    /// </summary>
    /// <returns>A string that represents the current ID.</returns>
    public override string ToString()
    {
        if (_type == IdType.Null)
            return "Null";

        return _type switch
        {
            IdType.SteamId => $"Steam:{((SteamId)_id).AccountId}",
            IdType.UInt => $"Uint:{_id}",
            IdType.IPEndPoint => $"Id:{(IPEndPoint)_id}",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets the underlying type of the stored ID.
    /// </summary>
    public Type UnderlyingType => _type switch
    {
        IdType.SteamId => typeof(SteamId),
        IdType.UInt => typeof(uint),
        IdType.IPEndPoint => typeof(IPEndPoint),
        _ => typeof(object)
    };

    /// <summary>
    /// Gets the underlying value of the stored ID.
    /// </summary>
    public object UnderlyingValue => _id;
}
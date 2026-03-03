using Il2CppSteamworks;
using ReplantedOnline.Enums;
using System.Net;

namespace ReplantedOnline.Structs;

/// <summary>
/// Unified identifier supporting SteamId (Steam), uint (simplified IDs), and IPEndPoint (LAN).
/// </summary>
internal readonly struct ID : IEquatable<ID>, IComparable<ID>
{
    private readonly object _id;
    private readonly IdType _type;

    internal static ID Null { get; } = new ID(null, IdType.Null);

    internal ID(object id, IdType type)
    {
        _id = id;
        _type = type;
    }

    internal bool IsNull => _type == IdType.Null;
    internal bool IsSteamId => _type == IdType.SteamId;
    internal bool IsUInt => _type == IdType.UInt;
    internal bool IsIPEndPoint => _type == IdType.IPEndPoint;
    internal bool HasValue => _type != IdType.Null;

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

    internal SteamId AsSteamId()
    {
        if (TryGetSteamId(out var id))
            return id;
        throw new InvalidCastException($"ID is not a SteamId (current type: {_type})");
    }

    internal uint AsUInt()
    {
        if (TryGetUInt(out var id))
            return id;
        throw new InvalidCastException($"ID is not a uint (current type: {_type})");
    }

    internal IPEndPoint AsIPEndPoint()
    {
        if (TryGetIPEndPoint(out var endpoint))
            return endpoint;
        throw new InvalidCastException($"ID is not an IPEndPoint (current type: {_type})");
    }

    public static bool operator ==(ID left, ID right) => left.Equals(right);
    public static bool operator !=(ID left, ID right) => !left.Equals(right);

    public static implicit operator ID(SteamId steamId) => new(steamId, IdType.SteamId);
    public static implicit operator ID(uint id) => new(id, IdType.UInt);
    public static implicit operator ID(IPEndPoint endpoint) => new(endpoint, IdType.IPEndPoint);

    public static explicit operator SteamId(ID id) => id.AsSteamId();
    public static explicit operator uint(ID id) => id.AsUInt();
    public static explicit operator IPEndPoint(ID id) => id.AsIPEndPoint();

    public override bool Equals(object obj) => obj is ID other && Equals(other);

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

    public Type UnderlyingType => _type switch
    {
        IdType.SteamId => typeof(SteamId),
        IdType.UInt => typeof(uint),
        IdType.IPEndPoint => typeof(IPEndPoint),
        _ => typeof(object)
    };

    public object UnderlyingValue => _id;
}
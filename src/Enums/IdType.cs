namespace ReplantedOnline.Enums;

/// <summary>
/// Specifies the type of identifier used to represent something, such as a Steam ID, unsigned integer, or IP endpoint.
/// </summary>
internal enum IdType : byte
{
    /// <summary>
    /// Represents a null or invalid identifier with no value.
    /// </summary>
    Null,

    /// <summary>
    /// Represents a Steam identifier (SteamId) used for Steamworks networking.
    /// </summary>
    SteamId,

    /// <summary>
    /// Represents a simple unsigned 32-bit integer identifier, useful for testing or simplified ID systems.
    /// </summary>
    UInt,

    /// <summary>
    /// Represents an IP endpoint (IP address and port) identifier, used for LAN or direct IP networking.
    /// </summary>
    IPEndPoint
}
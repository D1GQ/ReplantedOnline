namespace ReplantedOnline.Enums.Network;

/// <summary>
/// Defines the transport mode for network communication.
/// </summary>
internal enum TransportMode
{
    /// <summary>
    /// Uses Steam's peer-to-peer networking for communication.
    /// </summary>
    Steam,

    /// <summary>
    /// Uses a local area network (LAN) for communication.
    /// </summary>
    Lan
}

namespace ReplantedOnline.Enums.Network;

/// <summary>
/// Describes how the local peer is connecting to the network.
/// </summary>
internal enum ConnectionMode
{
    /// <summary>
    /// Not connected.
    /// </summary>
    None,

    /// <summary>
    /// Hosting a lobby and broadcasting it on the LAN.
    /// </summary>
    LanHost,

    /// <summary>
    /// Joining a lobby discovered via LAN broadcast.
    /// </summary>
    LanClient,

    /// <summary>
    /// Hosting a lobby without broadcasting (direct IP only).
    /// </summary>
    DirectHost,

    /// <summary>
    /// Joining a lobby by explicit IP/hostname and port.
    /// </summary>
    DirectClient,

    /// <summary>
    /// Reserved for future relay/matchmaking transport.
    /// </summary>
    RelayHost,

    /// <summary>
    /// Reserved for future relay/matchmaking transport.
    /// </summary>
    RelayClient,
}
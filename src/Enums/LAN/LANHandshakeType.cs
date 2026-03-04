namespace ReplantedOnline.Enums.LAN;

/// <summary>
/// Defines the types of handshake messages used during LAN connection establishment.
/// </summary>
internal enum LanHandshakeType : byte
{
    /// <summary>
    /// Initial connection request from a client to a host.
    /// Sent when a client wants to join a lobby.
    /// </summary>
    Request,

    /// <summary>
    /// Acceptance response from host to client.
    /// Indicates the connection request was approved.
    /// </summary>
    Accept,

    /// <summary>
    /// Notification that a client is leaving the lobby.
    /// Sent when a client disconnects or is disconnected.
    /// </summary>
    Leave
}
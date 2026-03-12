namespace ReplantedOnline.Enums.LAN;

/// <summary>
/// Defines the types of handshake messages used during LAN connection establishment.
/// </summary>
internal enum LanHandshakeType : byte
{
    // Client requesting to join
    Request,

    // Host accepting client
    Accept,

    // Host rejecting client
    Reject,

    // Client leaving or being disconnected
    Leave
}
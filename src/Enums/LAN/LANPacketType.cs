namespace ReplantedOnline.Enums.LAN;

/// <summary>
/// Defines the types of internal LAN packets used for network communication.
/// These packets are separate from regular RPC packets and handle lobby management.
/// </summary>
internal enum LanPacketType : byte
{
    /// <summary>
    /// Handshake packets for connection establishment (Request, Accept, Leave).
    /// Used to manage client connections to the lobby.
    /// </summary>
    Handshake,

    /// <summary>
    /// Client information packets containing player details like name and ID.
    /// Sent when a client joins to broadcast their information.
    /// </summary>
    ClientInfo,

    /// <summary>
    /// Lobby data packets containing global lobby settings and properties.
    /// Includes data like lobby name, max players, joinable status, etc.
    /// </summary>
    LobbyData,

    /// <summary>
    /// Member data packets containing per-player custom data.
    /// Used for player-specific information like ready status, team selection, etc.
    /// </summary>
    MemberData
}
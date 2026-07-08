namespace ReplantedOnline.Enums.Network;

/// <summary>
/// Identifies the type of network packet for proper routing and handling.
/// </summary>
internal enum PacketType
{
    /// <summary>
    /// No specific tag or unhandled packet type.
    /// </summary>
    None,

    /// <summary>
    /// LAN packet used for local network discovery and communication between clients on the same network.
    /// </summary>
    Server,

    /// <summary>
    /// Packet used removing a client from the game.
    /// </summary>
    RemoveClient,

    /// <summary>
    /// Packet used resetting lobby
    /// </summary>
    ResetLobby,

    /// <summary>
    /// Remote Procedure Call packet for executing methods on remote clients.
    /// </summary>
    Rpc,

    /// <summary>
    /// Packet used for requesting host to spawn a network object.
    /// </summary>
    NetworkObjectSpawnCmd,

    /// <summary>
    /// Packet used for host to spawn a network object.
    /// </summary>
    NetworkObjectSpawn,

    /// <summary>
    /// Packet used for host rejecting network objects spawn request.
    /// </summary>
    NetworkObjectReject,

    /// <summary>
    /// Packet used for despawning a network object.
    /// </summary>
    NetworkObjectDespawn,

    /// <summary>
    /// Packet used for syncing a network object.
    /// </summary>
    NetworkObjectSync,

    /// <summary>
    /// Packet used for P2P session establishment and maintenance on a rpc receiver object.
    /// </summary>
    ObjectRpc,
}
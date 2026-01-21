namespace ReplantedOnline.Enums;

/// <summary>
/// Identifies the type of network packet for proper routing and handling.
/// Used to distinguish between different packet categories in the networking system.
/// </summary>
internal enum PacketTag
{
    /// <summary>
    /// No specific tag or unhandled packet type.
    /// </summary>
    None,

    /// <summary>
    /// Packet used removing a client from the game.
    /// </summary>
    RemoveClient,

    /// <summary>
    /// Remote Procedure Call packet for executing methods on remote clients.
    /// </summary>
    Rpc,

    /// <summary>
    /// Packet used for spawning a network object.
    /// </summary>
    NetworkClassSpawn,

    /// <summary>
    /// Packet used for despawning a network object.
    /// </summary>
    NetworkClassDespawn,

    /// <summary>
    /// Packet used for syncing a network object.
    /// </summary>
    NetworkClassSync,

    /// <summary>
    /// Packet used for P2P session establishment and maintenance on a network object.
    /// </summary>
    NetworkClassRpc,
}
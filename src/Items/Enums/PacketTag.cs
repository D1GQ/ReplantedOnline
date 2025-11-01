namespace ReplantedOnline.Items.Enums;

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
    /// Packet used for P2P session establishment and maintenance.
    /// </summary>
    P2P,

    /// <summary>
    /// Remote Procedure Call packet for executing methods on remote clients.
    /// </summary>
    Rpc
}
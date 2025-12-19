namespace ReplantedOnline.Enums;

/// <summary>
/// Defines the communication channels used for packet transmission in the networking system.
/// </summary>
internal enum PacketChannel
{
    /// <summary>
    /// The main channel for regular, reliable packet delivery.
    /// </summary>
    Main,

    /// <summary>
    /// The buffered channel for packets that can be queued and processed later.
    /// </summary>
    Buffered,

    /// <summary>
    /// The Remote Procedure Call (RPC) channel for method invocation between client and server.
    /// </summary>
    Rpc
}
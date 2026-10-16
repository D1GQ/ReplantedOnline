namespace ReplantedOnline.Enums.Network;

/// <summary>
/// Specifies when the original method of an RPC should be executed locally
/// relative to sending the RPC to remote clients.
/// </summary>
internal enum LocalExecution
{
    /// <summary>
    /// The original method is not executed locally. Only remote clients will run it.
    /// </summary>
    None,

    /// <summary>
    /// The original method is executed locally before the RPC is sent.
    /// </summary>
    Before,

    /// <summary>
    /// The original method is executed locally after the RPC has been sent.
    /// </summary>
    After
}
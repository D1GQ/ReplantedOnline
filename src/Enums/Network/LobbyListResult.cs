namespace ReplantedOnline.Enums.Network;

/// <summary>
/// Represents the possible results of a lobby list retrieval operation.
/// </summary>
internal enum LobbyListResult
{
    /// <summary>
    /// The lobby list was successfully retrieved.
    /// </summary>
    Succeed,

    /// <summary>
    /// No lobbies were found matching the search criteria.
    /// </summary>
    Failed,

    /// <summary>
    /// An error occurred during the lobby search operation.
    /// </summary>
    Error
}
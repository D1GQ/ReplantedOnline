namespace ReplantedOnline.Enums;

/// <summary>
/// Represents error states that can occur during lobby list operations.
/// </summary>
internal enum LobbyListError
{
    /// <summary>
    /// No lobbies were found matching the search criteria.
    /// </summary>
    NoneFound,

    /// <summary>
    /// An error occurred during the lobby search operation.
    /// </summary>
    Error
}
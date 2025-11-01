namespace ReplantedOnline.Items.Enums;

/// <summary>
/// Defines the types of Remote Procedure Calls (RPCs) available in ReplantedOnline.
/// RPCs are used to execute specific game logic on remote clients.
/// </summary>
internal enum RpcType
{
    /// <summary>
    /// Initiates the start of a game match with the specified parameters.
    /// </summary>
    StartGame,

    /// <summary>
    /// Updates the current game state on all connected clients.
    /// </summary>
    UpdateGameState,
}
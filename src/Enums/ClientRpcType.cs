namespace ReplantedOnline.Enums;

/// <summary>
/// Defines the types of Remote Procedure Calls (RPCs) available in ReplantedOnline.
/// RPCs are used to execute specific game logic on remote clients.
/// </summary>
internal enum ClientRpcType
{
    /// <summary>
    /// Initiates the start of a game match with the specified parameters.
    /// </summary>
    StartGame,

    /// <summary>
    /// Updates the current game state on all connected clients.
    /// </summary>
    UpdateGameState,

    /// <summary>
    /// Sync when a player chooses their seed/plant selection.
    /// </summary>
    ChooseSeed,

    /// <summary>
    /// Sync starting a mower
    /// </summary>
    StartMower,

    /// <summary>
    /// Sync adding a ladder to a plant
    /// </summary>
    AddLadder,
}
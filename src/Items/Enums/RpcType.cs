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

    /// <summary>
    /// Sync when a player chooses their seed/plant selection.
    /// </summary>
    ChooseSeed,

    /// <summary>
    /// Sync when a packet gose on cooldown.
    /// </summary>
    SetSeedPacketCooldown,

    /// <summary>
    /// Sync when all seedpackets on a players side.
    /// </summary>
    SetSeedPackets,

    /// <summary>
    /// Sync starting a mower
    /// </summary>
    MowZombie,

    /// <summary>
    /// Add or remove an amount of money from the opposite player
    /// </summary>
    SyncOpponentMoney
}
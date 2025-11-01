namespace ReplantedOnline.Items.Enums;

/// <summary>
/// Represents the various states of a Plants vs. Zombies: Replanted online game session.
/// Tracks the current phase of gameplay for synchronization between players.
/// </summary>
internal enum GameState
{
    /// <summary>
    /// Players are in the lobby, preparing to start a match.
    /// </summary>
    Lobby,

    /// <summary>
    /// The plant player is currently selecting their seeds.
    /// </summary>
    PlantChoosingSeed,

    /// <summary>
    /// The zombie player is currently selecting their seeds.
    /// </summary>
    ZombieChoosingSeed,

    /// <summary>
    /// Active gameplay is in progress with both players controlling their units.
    /// </summary>
    Gameplay
}
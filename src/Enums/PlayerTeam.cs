namespace ReplantedOnline.Enums;

/// <summary>
/// Represents the team affiliation of a player in the game.
/// </summary>
internal enum PlayerTeam
{
    /// <summary>
    /// Unassigned.
    /// </summary>
    None,

    /// <summary>
    /// The Plants team.
    /// </summary>
    Plants,

    /// <summary>
    /// The Zombies team.
    /// </summary>
    Zombies,

    /// <summary>
    /// Spectators who are observing the game.
    /// </summary>
    Spectators
}
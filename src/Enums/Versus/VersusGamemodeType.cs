namespace ReplantedOnline.Enums.Versus;

/// <summary>
/// Represents the different gamemode types available in versus mode.
/// </summary>
internal enum VersusGamemodeType
{
    /// <summary>
    /// Quickplay mode, Players select from predefined seed banks.
    /// </summary>
    Quickplay,

    /// <summary>
    /// Random mode, Seed banks are randomly generated for each match.
    /// </summary>
    Random,

    /// <summary>
    /// Custom mode, Players can fully customize their seed banks before the match starts.
    /// </summary>
    Custom
}
namespace ReplantedOnline.Enums.Network;

/// <summary>
/// Specifies the reasons for banning a player from a ReplantedOnline lobby.
/// Used to categorize and communicate ban reasons for moderation purposes.
/// </summary>
internal enum BanReasons
{
    /// <summary>
    /// Player was banned by the lobby host's discretion.
    /// </summary>
    ByHost,

    /// <summary>
    /// Player is permanently banned from the lobby.
    /// </summary>
    Banned,

    /// <summary>
    /// Player was banned for cheating or using unauthorized modifications.
    /// </summary>
    Cheating
}
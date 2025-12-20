namespace ReplantedOnline.Enums;

/// <summary>
/// Specifies the release type of the mod.
/// </summary>
internal enum ReleaseType
{
    /// <summary>
    /// A stable, production-ready release.
    /// </summary>
    release,

    /// <summary>
    /// A beta release for testing purposes.
    /// </summary>
    beta,

    /// <summary>
    /// A development build with the latest features and potential instability.
    /// </summary>
    dev
}
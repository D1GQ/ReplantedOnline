using ReplantedOnline.Enums;

namespace ReplantedOnline;

/// <summary>
/// Provides constant metadata and identification information for the Replanted Online mod.
/// </summary>
internal static class ModInfo
{
#if DEBUG
    internal const bool DEBUG = true;
#else
    internal const bool DEBUG = false;
#endif

    /// <summary>
    /// The display name of the mod as shown to users in mod managers and in-game menus.
    /// </summary>
    internal const string MOD_NAME = "Replanted Online";

    /// <summary>
    /// The current version of the mod following semantic versioning (Major.Minor.Patch).
    /// </summary>
    internal const string MOD_VERSION = "1.0.0";

    /// <summary>
    /// The release type of the current mod version.
    /// </summary>
    internal const string MOD_RELEASE = nameof(ReleaseType.beta);

    /// <summary>
    /// The number of the release.
    /// </summary>
    internal const string MOD_RELEASE_INFO = "4";

    /// <summary>
    /// The formatted version string of the mod using semantic versioning.
    /// Format: vMajor.Minor.Patch-prereleaseNumber.
    /// </summary>
    internal const string MOD_VERSION_FORMATTED = $"{MOD_VERSION}-{MOD_RELEASE}{MOD_RELEASE_INFO}";

    /// <summary>
    /// The date when this version was released, formatted as mm.dd.yyyy.
    /// </summary>
    internal const string RELEASE_DATE = "12.24.2025";

    /// <summary>
    /// The unique identifier for the mod following reverse domain name notation.
    /// </summary>
    internal const string MOD_GUID = "com.d1gq.replantedonline";

    /// <summary>
    /// The link for the github page.
    /// </summary>
    internal const string GITHUB = "https://github.com/D1GQ/ReplantedOnline";

    /// <summary>
    /// That's ME!
    /// </summary>
    internal const string CREATOR = "D1GQ";

    /// <summary>
    /// List of all contributors, separate by ",".
    /// </summary>
    internal const string CONTRIBUTORS = "PalmForest";

    /// <summary>
    /// Contains constants related to Plants vs. Zombies™: Replanted game information.
    /// </summary>
    internal static class PVZR
    {
        /// <summary>
        /// The name of the company that developed the game.
        /// </summary>
        internal const string COMPANY = "PopCap Games";

        /// <summary>
        /// The official name of the game.
        /// </summary>
        internal const string GAME = "PvZ Replanted";
    }

    /// <summary>
    /// Contains constants related to the BloomEngine dependency.
    /// </summary>
    internal static class BloomEngine
    {
        /// <summary>
        /// Dependency name for BloomEngine.
        /// </summary>
        internal const string BLOOM_ENGINE_DEPENDENCY = "BloomEngine";
    }
}
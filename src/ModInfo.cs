namespace ReplantedOnline;

/// <summary>
/// Provides constant metadata and identification information for the Replanted Online mod.
/// </summary>
public static class ModInfo
{
    /// <summary>
    /// The display name of the mod as shown to users in mod managers and in-game menus.
    /// </summary>
    public const string MOD_NAME = "Replanted Online";

    /// <summary>
    /// The current version of the mod following semantic versioning (Major.Minor.Patch).
    /// </summary>
    public const string MOD_VERSION = "1.0.0";

    /// <summary>
    /// The unique identifier for the mod following reverse domain name notation.
    /// </summary>
    public const string MOD_GUID = "com.d1gq.replantedonline";

    /// <summary>
    /// The link for the github page.
    /// </summary>
    public const string GITHUB = "https://github.com/D1GQ/ReplantedOnline";

    /// <summary>
    /// That's ME!
    /// </summary>
    public const string CREATOR = "D1GQ";

    /// <summary>
    /// List of all contributors, separate by ",".
    /// </summary>
    public const string CONTRIBUTORS = "PalmForest";

    /// <summary>
    /// Contains constants related to Plants vs. Zombies™: Replanted game information.
    /// </summary>
    public static class PVZR
    {
        /// <summary>
        /// The name of the company that developed the game.
        /// </summary>
        public const string COMPANY_NAME = "PopCap Games";

        /// <summary>
        /// The official name of the game.
        /// </summary>
        public const string GAME_NAME = "Plants vs. Zombies™: Replanted";
    }

    /// <summary>
    /// Contains constants related to the BloomEngine dependency.
    /// </summary>
    public static class BloomEngine
    {
        /// <summary>
        /// Dependency name for BloomEngine.
        /// </summary>
        public const string BLOOM_ENGINE_DEPENDENCY = "BloomEngine";
    }
}
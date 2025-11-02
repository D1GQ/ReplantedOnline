namespace ReplantedOnline;

/// <summary>
/// Provides constant metadata and identification information for the Replanted Online mod.
/// This class contains static read-only properties that define the mod's basic information
/// used by MelonLoader for registration, display, and dependency management.
/// </summary>
public class ModInfo
{
    /// <summary>
    /// The display name of the mod as shown to users in mod managers and in-game menus.
    /// </summary>
    public const string ModName = "Replanted Online";

    /// <summary>
    /// The current version of the mod following semantic versioning (Major.Minor.Patch).
    /// </summary>
    public const string ModVersion = "1.0.0";

    /// <summary>
    /// The unique identifier for the mod following reverse domain name notation.
    /// This GUID is used for dependency resolution and mod identification.
    /// </summary>
    public const string ModGUID = "com.d1gq.replantedonline";
}
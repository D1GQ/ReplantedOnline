using ReplantedOnline.Enums.Modded;
using ReplantedOnline.Structs;
using System.Reflection;

namespace ReplantedOnline;

/// <inheritdoc/>
internal partial class ReplantedOnlineMod
{
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
        internal const string MOD_RELEASE_INFO = "15";

        /// <summary>
        /// The hotfix number for hotfix releases.
        /// </summary>
        internal const string HOTFIX_NUM = "0";

        /// <summary>
        /// The formatted version string of the mod using semantic versioning.
        /// </summary>
#if DEBUG
        internal const string MOD_VERSION_FORMATTED = $"{MOD_VERSION}-debug-{MOD_RELEASE}{MOD_RELEASE_INFO}";
#elif HOTFIX
    internal const string MOD_VERSION_FORMATTED = $"{MOD_VERSION}-{MOD_RELEASE}{MOD_RELEASE_INFO}-hotfix{HOTFIX_NUM}";
#else
    internal const string MOD_VERSION_FORMATTED = $"{MOD_VERSION}-{MOD_RELEASE}{MOD_RELEASE_INFO}";
#endif

        /// <summary>
        /// The date when this version was released, formatted as mm.dd.yyyy.
        /// </summary>
        internal const string RELEASE_DATE = ThisAssembly.Metadata.BuildDate;

        /// <summary>
        /// The unique identifier for the mod following reverse domain name notation.
        /// </summary>
        internal const string MOD_GUID = "com.d1gq.replantedonline";

        /// <summary>
        /// The link for the github page.
        /// </summary>
        internal const string GITHUB = ThisAssembly.Git.RepositoryUrl;

        /// <summary>
        /// The commit hash for the current build of the mod.
        /// </summary>
        internal const string GITHUB_COMMIT = ThisAssembly.Git.Commit;

        /// <summary>
        /// That's ME!
        /// </summary>
        internal const string CREATOR = "D1GQ";

        /// <summary>
        /// List of all contributors, separate by ",".
        /// </summary>
        internal const string CONTRIBUTORS = "PalmForest";

        /// <summary>
        /// The signature for the mod dll file.
        /// </summary>
        internal static readonly ModSignature ModSignature = new();

        /// <summary>
        /// The assembly associated to this mod.
        /// </summary>
        internal static Assembly Assembly
        {
            get
            {
                if (field == null)
                {
                    field = Assembly.GetExecutingAssembly();
                }
                return field;
            }
        }

        /// <summary>
        /// Contains constants related to Plants vs. Zombies™: Replanted game information.
        /// </summary>
        internal static class Replanted
        {
            /// <summary>
            /// The name of the company that developed the game.
            /// </summary>
            internal const string COMPANY = "PopCap Games";

            /// <summary>
            /// The official name of the game.
            /// </summary>
            internal const string GAME = "PvZ Replanted";

            /// <summary>
            /// The current supported versions, separate by ",".
            /// </summary>
            internal const string SUPPORTED_VERSIONS = "1.5.1*";
        }

        /// <summary>
        /// Contains constants related to the BloomEngine dependency.
        /// </summary>
        internal static class BloomEngine
        {
            /// <summary>
            /// Name for BloomEngine.
            /// </summary>
            internal const string BLOOM_ENGINE_NAME = "BloomEngine";
        }

        /// <summary>
        /// Contains constants related to the DiscordRPC optional dependency.
        /// </summary>
        internal static class DiscordRPC
        {
            /// <summary>
            /// Name for DiscordRPC.
            /// </summary>
            internal const string DISCORD_RPC_NAME = "DiscordRPC";

            /// <summary>
            /// The link for the github page.
            /// </summary>
            internal const string GITHUB = "https://github.com/Lachee/discord-rpc-csharp";
        }
    }
}

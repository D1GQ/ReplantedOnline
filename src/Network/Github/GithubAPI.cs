using Il2CppInterop.Runtime.Attributes;
using MelonLoader;
using ReplantedOnline.Data;
using ReplantedOnline.Data.Json;
using ReplantedOnline.Data.Json.Config.Reloaded;
using ReplantedOnline.Modules.Modded;
using ReplantedOnline.Utilities.Unity;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Github;

/// <summary>
/// Singleton MonoBehaviour responsible for managing GitHub API interactions.
/// </summary>
[RegisterTypeInIl2Cpp]
internal sealed class GithubAPI : MonoBehaviour
{
    /// <summary>
    /// Gets the current download progress as a value between 0 and 1.
    /// </summary>
    internal float Progress { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the GitHub API data has been fully loaded and processed.
    /// </summary>
    internal bool IsReady { get; private set; }

    /// <summary>
    /// Establishes a connection to the GitHub API and begins downloading configuration data.
    /// </summary>
    internal void Connect()
    {
        this.StartCoroutine(CoConnect());
    }

    /// <summary>
    /// Coroutine that handles the connection to GitHub and downloads the manifest and configuration files.
    /// </summary>
    /// <returns>An IEnumerator for coroutine execution.</returns>
    [HideFromIl2Cpp]
    private IEnumerator CoConnect()
    {
        string manifestJson = string.Empty;
        yield return GitHubFile.CoDownloadManifest(GitUrlPath.RepositoryApi.Combine("manifest.json"), json =>
        {
            manifestJson = json;
            Progress = 1f / 2f;
        }, progress =>
        {
            Progress = progress / 2f;
        });

        Manifest githubManifest = new();
        githubManifest.Deserialize(manifestJson);

        if (githubManifest != null)
        {
            foreach (var versusModeConfigOverride in githubManifest.VersusModeConfigOverrides)
            {
                if (TextHandler.CheckWildcardPrefix(ReplantedOnlineMod.ModInfo.MOD_VERSION_FORMATTED, versusModeConfigOverride.Version))
                {
                    yield return CoDownloadVersusModeConfig(versusModeConfigOverride.FileName);
                    break;
                }
            }

            Progress = 1f;
        }

        IsReady = true;
    }

    /// <summary>
    /// Coroutine that downloads a specific versus mode configuration file from GitHub.
    /// </summary>
    /// <param name="fileName">The name of the configuration file to download.</param>
    /// <returns>An IEnumerator for coroutine execution.</returns>
    [HideFromIl2Cpp]
    private IEnumerator CoDownloadVersusModeConfig(string fileName)
    {
        string configJson = string.Empty;
        yield return GitHubFile.CoDownloadManifest(GitUrlPath.RepositoryConfigs.Combine(fileName), json =>
        {
            configJson = json;
            Progress = 1f;
        }, progress =>
        {
            Progress = (progress / 2f) + 0.5f;
        });
        var config = new VersusModeConfig();
        config.Deserialize(configJson);
        DataManager.VersusModeConfig = config;
    }

    /// <summary>
    /// Represents the GitHub manifest containing version-specific configuration overrides.
    /// </summary>
    private sealed class Manifest : JsonObject
    {
        /// <summary>
        /// Gets or sets the collection of versus mode configuration overrides.
        /// </summary>
        public List<VersusModeConfigOverride> VersusModeConfigOverrides { get; set; } = [];
    }

    /// <summary>
    /// Represents a version-specific override for the versus mode configuration.
    /// </summary>
    private sealed class VersusModeConfigOverride : JsonObject
    {
        /// <summary>
        /// Gets or sets the filename of the configuration file to download.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the version pattern that this override applies to.
        /// Supports wildcard prefixes with an asterisk (*).
        /// </summary>
        public string Version { get; set; } = string.Empty;
    }
}
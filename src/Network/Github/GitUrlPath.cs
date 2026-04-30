#pragma warning disable IDE0251

using System.Collections;

namespace ReplantedOnline.Network.Github;

/// <summary>
/// Represents a GitHub repository URL path structure for constructing raw content URLs.
/// </summary>
/// <param name="folder">The base folder path within the GitHub repository.</param>
internal struct GitUrlPath(string folder)
{
    private const string BASE_URL = "https://raw.githubusercontent.com/D1GQ/ReplantedOnline";

    /// <summary>
    /// The default branch name used for the repository (typically "main").
    /// </summary>
    internal const string BRANCH = "main";

    /// <summary>
    /// Gets the primary repository URL path pointing to the main branch.
    /// </summary>
    internal static readonly GitUrlPath Repository = new(BRANCH);

    /// <summary>
    /// Gets the API repository URL path pointing to the api folder under the main branch.
    /// </summary>
    internal static readonly GitUrlPath RepositoryApi = new($"{BRANCH}/api");

    private readonly string _folder = folder;

    /// <summary>
    /// Combines the base URL with the folder path and additional path segments.
    /// </summary>
    /// <param name="paths">Variable number of path segments to append to the URL.</param>
    /// <returns>A complete raw GitHub content URL.</returns>
    internal readonly string Combine(params string[] paths)
    {
        return $"{BASE_URL}/{_folder}/{string.Join("/", paths)}";
    }

    /// <summary>
    /// Returns the complete base URL for this GitHub path instance.
    /// </summary>
    /// <returns>A string representing the full raw GitHub content URL for this folder.</returns>
    public override readonly string ToString()
    {
        return $"{BASE_URL}/{_folder}";
    }

    /// <summary>
    /// Starts a coroutine to download a file from the constructed GitHub URL.
    /// </summary>
    /// <param name="path">Additional path segments to append to the base URL (relative file path within the repository).</param>
    /// <param name="localFilePath">The local file path where the downloaded file will be saved.</param>
    /// <param name="callback">Optional callback invoked after successful download, passing the local file path.</param>
    /// <returns>An IEnumerator suitable for use with StartCoroutine.</returns>
    internal IEnumerator CoDownloadFile(string path, string localFilePath, Action<string> callback = null)
    {
        return GitHubFile.CoDownloadFile(Combine(path), localFilePath, callback);
    }

    /// <summary>
    /// Starts a coroutine to download a manifest file from the constructed GitHub URL.
    /// </summary>
    /// <param name="path">Additional path segments to append to the base URL (manifest file path within the repository).</param>
    /// <param name="Callback">Action invoked after successful download, passing the manifest content as a string.</param>
    /// <returns>An IEnumerator suitable for use with StartCoroutine.</returns>
    internal IEnumerator CoDownloadManifest(string path, Action<string> Callback)
    {
        return GitHubFile.CoDownloadManifest(Combine(path), Callback);
    }
}
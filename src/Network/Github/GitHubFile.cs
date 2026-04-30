using ReplantedOnline.Utilities;
using System.Collections;
using UnityEngine.Networking;

namespace ReplantedOnline.Network.Github;

/// <summary>
/// Provides static methods for downloading files and manifests from GitHub repositories.
/// </summary>
internal static class GitHubFile
{
    /// <summary>
    /// Coroutine that downloads a file from a specified URL and saves it to the local file system.
    /// </summary>
    /// <param name="url">The URL of the file to download.</param>
    /// <param name="localFilePath">The local file path where the downloaded file will be saved.</param>
    /// <param name="callback">Optional callback invoked after successful download, passing the local file path.</param>
    /// <returns>An IEnumerator suitable for use with StartCoroutine.</returns>
    internal static IEnumerator CoDownloadFile(string url, string localFilePath, Action<string> callback = null)
    {
        var www = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
        {
            downloadHandler = new DownloadHandlerBuffer()
        };

        var operation = www.SendWebRequest();

        while (!operation.isDone)
        {
            yield return null;
        }

        if (www.result != UnityWebRequest.Result.Success)
        {
            ReplantedOnlineMod.Logger.Error(typeof(GitHubFile), $"Error downloading file from URL '{url}': {www.error} (Response Code: {(int)www.responseCode})");
            yield break;
        }

        byte[] bytes = www.downloadHandler.GetNativeData().ToArray();
        File.WriteAllBytes(localFilePath, bytes);

        ReplantedOnlineMod.Logger.Msg($"Saved file: {localFilePath}");
        callback?.Invoke(localFilePath);
    }

    /// <summary>
    /// Coroutine that downloads a manifest file from a specified URL and returns its content as a string.
    /// </summary>
    /// <param name="url">The URL of the manifest file to download.</param>
    /// <param name="Callback">Action invoked after successful download, passing the manifest content as a string.</param>
    /// <returns>An IEnumerator suitable for use with StartCoroutine.</returns>
    internal static IEnumerator CoDownloadManifest(string url, Action<string> Callback)
    {
        var www = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
        {
            downloadHandler = new DownloadHandlerBuffer()
        };
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            ReplantedOnlineMod.Logger.Error(typeof(GitHubFile), $"Error downloading {url}: {www.error}");
            yield break;
        }

        var response = www.downloadHandler.text;
        www.Dispose();
        Callback.Invoke(response);
    }
}
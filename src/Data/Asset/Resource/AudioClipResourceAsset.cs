using ReplantedOnline.Utilities.Modded;
using UnityEngine;
using static ReplantedOnline.ReplantedOnlineMod;

namespace ReplantedOnline.Data.Asset.Resource;

/// <summary>
/// Represents an audio clip asset loaded from embedded WAV resources.
/// </summary>
/// <param name="path">The resource path to the audio file.</param>
internal sealed class AudioClipResourceAsset(string path) : ResourceAsset<AudioClip>(path)
{
    /// <summary>
    /// Loads the audio clip asset from the mod's embedded resources.
    /// </summary>
    internal override void Load()
    {
        try
        {
            var asset = ModInfo.Assembly.LoadWavFromResources(ResourcePath);
            if (asset != null)
            {
                Loadded = true;
                Failed = false;
                asset.name = ResourcePath;
                Asset = asset;
            }
            else
            {
                Loadded = false;
                Failed = true;
            }
        }
        catch
        {
            Loadded = false;
            Failed = true;
        }
    }
}
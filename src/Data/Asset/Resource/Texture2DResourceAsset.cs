using ReplantedOnline.Utilities.Modded;
using UnityEngine;

namespace ReplantedOnline.Data.Asset.Resource;

internal sealed class Texture2DResourceAsset(string path) : ResourceAsset<Texture2D>(path)
{
    /// <summary>
    /// Loads the sprite asset from the mod's embedded resources.
    /// </summary>
    internal override void Load()
    {
        try
        {
            var asset = ReplantedOnlineMod.ModInfo.Assembly.LoadTextureFromResources(ResourcePath);
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
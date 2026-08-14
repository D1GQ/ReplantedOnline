using ReplantedOnline.Utilities.Modded;
using UnityEngine;

namespace ReplantedOnline.Data.Asset.Resource;

/// <summary>
/// Represents a sprite asset loaded from embedded resources.
/// </summary>
/// <param name="path">The resource path to the sprite image file.</param>
/// <param name="pixelsPerUnit">The number of pixels per unit for the sprite. Default is 1f.</param>
internal sealed class SpriteResourceAsset(string path, float pixelsPerUnit = 1f) : ResourceAsset<Sprite>(path)
{
    /// <summary>
    /// The number of pixels per unit for the sprite.
    /// </summary>
    private readonly float _pixelsPerUnit = pixelsPerUnit;

    /// <summary>
    /// Loads the sprite asset from the mod's embedded resources.
    /// </summary>
    internal override void Load()
    {
        try
        {
            var asset = ReplantedOnlineMod.ModInfo.Assembly.LoadSpriteFromResources(ResourcePath, _pixelsPerUnit);
            if (asset != null)
            {
                Loadded = true;
                Failed = false;
                asset.name = ResourcePath;
                asset.texture.name = ResourcePath;
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
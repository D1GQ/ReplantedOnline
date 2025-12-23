using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace ReplantedOnline.Helper;

/// <summary>
/// Provides utility methods for loading and managing images and sprites from various sources.
/// </summary>
internal static class ImageUtils
{
    /// <summary>
    /// Dictionary for caching loaded sprites to improve performance by avoiding duplicate loads.
    /// </summary>
    private static readonly Dictionary<int, Sprite> _cachedSprites = [];

    /// <summary>
    /// Loads a sprite from an embedded resource in the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly containing the embedded image resource.</param>
    /// <param name="resourcePath">The fully qualified name of the embedded resource.</param>
    /// <param name="pixelsPerUnit">The number of texture pixels that correspond to one unit in world space. Default is 1.</param>
    /// <returns>A Sprite loaded from the embedded resource, or null if loading fails.</returns>
    internal static Sprite LoadSpriteFromResources(this Assembly assembly, string resourcePath, float pixelsPerUnit = 1f)
    {
        try
        {
            var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
                return null;

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return LoadSpriteFromBytes(ms.ToArray());
        }
        catch (Exception ex)
        {
            MelonLogger.Error(ex);
            return null;
        }
    }

    /// <summary>
    /// Loads a sprite from a file on disk.
    /// </summary>
    /// <param name="filePath">The full path to the image file.</param>
    /// <param name="pixelsPerUnit">The number of texture pixels that correspond to one unit in world space. Default is 1.</param>
    /// <returns>A Sprite loaded from the disk file, or null if loading fails.</returns>
    internal static Sprite LoadSpriteFromDisk(string filePath, float pixelsPerUnit = 1f)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            return LoadSpriteFromBytes(bytes, pixelsPerUnit);
        }
        catch (Exception ex)
        {
            MelonLogger.Error(ex);
            return null;
        }
    }

    /// <summary>
    /// Loads a sprite from raw byte data, with built-in caching to prevent duplicate loads.
    /// </summary>
    /// <param name="bytes">The raw image file bytes (PNG, JPG, etc.).</param>
    /// <param name="pixelsPerUnit">The number of texture pixels that correspond to one unit in world space. Default is 1.</param>
    /// <returns>A Sprite created from the byte data, retrieved from cache if previously loaded.</returns>
    internal static Sprite LoadSpriteFromBytes(byte[] bytes, float pixelsPerUnit = 1f)
    {
        try
        {
            var cacheKey = ComputeContentHash(bytes, pixelsPerUnit);
            if (_cachedSprites.TryGetValue(cacheKey, out var sprite))
                return sprite;

            var texture = LoadTextureFromBytes(bytes);
            if (texture == null)
                return null;

            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;

            return _cachedSprites[cacheKey] = sprite;
        }
        catch (Exception ex)
        {
            MelonLogger.Error(ex);
            return null;
        }
    }

    /// <summary>
    /// Loads a Texture2D from raw byte data.
    /// </summary>
    /// <param name="bytes">The raw image file bytes (PNG, JPG, etc.).</param>
    /// <returns>A Texture2D created from the byte data, or null if loading fails.</returns>
    internal static Texture2D LoadTextureFromBytes(byte[] bytes)
    {
        try
        {
            Texture2D texture = new(1, 1, TextureFormat.ARGB32, false);
            using (var ms = new MemoryStream())
            {
                if (!texture.LoadImage(bytes, false))
                    return null;
            }

            return texture;
        }
        catch (Exception ex)
        {
            MelonLogger.Error(ex);
            return null;
        }
    }

    /// <summary>
    /// Computes a deterministic hash for byte array content and pixelsPerUnit value for caching purposes.
    /// </summary>
    /// <param name="bytes">The byte array to hash.</param>
    /// <param name="pixelsPerUnit">The pixelsPerUnit value to include in the hash.</param>
    /// <returns>An integer hash value representing the content.</returns>
    private static int ComputeContentHash(byte[] bytes, float pixelsPerUnit)
    {
        unchecked
        {
            int hash = (int)BitConverter.ToUInt32(bytes, 0) ^ bytes.Length;
            int step = Math.Max(1, bytes.Length / 32);
            for (int i = 0; i < bytes.Length; i += step)
            {
                hash = (hash * 397) ^ bytes[i];
            }
            int ppuHash = pixelsPerUnit.GetHashCode();
            hash = (hash * 397) ^ ppuHash;
            return hash;
        }
    }
}
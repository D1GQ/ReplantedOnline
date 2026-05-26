using System.Reflection;
using UnityEngine;

namespace ReplantedOnline.Utilities.Modded;

/// <summary>
/// Provides utility methods for loading and managing images and sprites from various sources.
/// </summary>
internal static class ImageUtils
{
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
            var sprite = LoadSpriteFromBytes(ms.ToArray(), pixelsPerUnit);
            sprite.name = Path.GetFileNameWithoutExtension(resourcePath);
            return sprite;
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(ex);
            return null;
        }
    }

    /// <summary>
    /// Loads a single sprite from a sprite sheet by its column and row index (1-based).
    /// </summary>
    /// <param name="texture">The texture sheet to extract the sprite from.</param>
    /// <param name="columns">Total columns in the sheet.</param>
    /// <param name="rows">Total rows in the sheet.</param>
    /// <param name="targetSprite">Column index (1-based) and Row index (1-based) of the desired sprite.</param>
    /// <param name="pixelsPerUnit">Pixels per unit for the sprite.</param>
    /// <param name="padding">Optional padding between sprites in pixels.</param>
    /// <returns>The requested Sprite, or null if out of bounds or failed to load.</returns>
    internal static Sprite LoadSpriteFromTextureSheet(this Texture2D texture, int columns, int rows, (int col, int row) targetSprite, float pixelsPerUnit = 1f, int padding = 0)
    {
        try
        {
            if (texture == null)
            {
                return null;
            }

            int colIndex = targetSprite.col - 1;
            int rowIndex = targetSprite.row - 1;

            if (colIndex >= columns || rowIndex >= rows || colIndex < 0 || rowIndex < 0)
            {
                ReplantedOnlineMod.Logger.Warning($"Sprite index ({targetSprite.col},{targetSprite.row}) is out of bounds for sheet with {columns}x{rows}!");
                return null;
            }

            int spriteWidth = (texture.width - (padding * (columns - 1))) / columns;
            int spriteHeight = (texture.height - (padding * (rows - 1))) / rows;

            Rect rect = new(
                colIndex * (spriteWidth + padding),
                (rows - 1 - rowIndex) * (spriteHeight + padding),
                spriteWidth,
                spriteHeight
            );

            Sprite sprite = Sprite.Create(
                texture,
                rect,
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit
            );
            sprite.name = texture.name + $"({targetSprite.col}, {targetSprite.row})";
            sprite.hideFlags |= HideFlags.HideAndDontSave;

            return sprite;
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(ex);
            return null;
        }
    }

    /// <summary>
    /// Loads a texture from an embedded resource.
    /// </summary>
    /// <param name="assembly">The assembly containing the embedded resource.</param>
    /// <param name="resourcePath">The fully qualified name of the embedded resource.</param>
    /// <returns>A Texture2D loaded from the resource, or null if loading fails.</returns>
    internal static Texture2D LoadTextureFromResources(this Assembly assembly, string resourcePath)
    {
        try
        {
            var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
                return null;

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            var texture = LoadTextureFromBytes(ms.ToArray());
            texture.name = Path.GetFileNameWithoutExtension(resourcePath);
            return texture;
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(ex);
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
            var sprite = LoadSpriteFromBytes(bytes, pixelsPerUnit);
            sprite.name = Path.GetFileNameWithoutExtension(Path.GetFileName(filePath));
            return sprite;
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(ex);
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
            var texture = LoadTextureFromBytes(bytes);
            if (texture == null)
                return null;

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;

            return sprite;
        }
        catch (Exception ex)
        {
            ReplantedOnlineMod.Logger.Error(ex);
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
            ReplantedOnlineMod.Logger.Error(ex);
            return null;
        }
    }
}
using ReplantedOnline.Utilities.Modded;
using UnityEngine;

namespace ReplantedOnline.Data.Asset.Resource;

/// <summary>
/// Represents a sprite sheet asset loaded from embedded resources that extracts multiple sprites.
/// </summary>
/// <param name="path">The resource path to the sprite sheet image file.</param>
/// <param name="rows">The number of rows in the sprite sheet grid.</param>
/// <param name="columns">The number of columns in the sprite sheet grid.</param>
/// <param name="pixelsPerUnit">The number of pixels per unit for the sprites. Default is 1f.</param>
internal sealed class SpriteSheetResourceAsset(string path, int rows, int columns, float pixelsPerUnit = 1f) : ResourceAsset<Sprite[]>(path)
{
    /// <summary>
    /// The number of rows in the sprite sheet grid.
    /// </summary>
    private readonly int _rows = rows;

    /// <summary>
    /// The number of columns in the sprite sheet grid.
    /// </summary>
    private readonly int _columns = columns;

    /// <summary>
    /// The number of pixels per unit for the sprites.
    /// </summary>
    private readonly float _pixelsPerUnit = pixelsPerUnit;

    /// <summary>
    /// Loads the sprite sheet from the mod's embedded resources and extracts individual sprites.
    /// </summary>
    internal override void Load()
    {
        try
        {
            // Load the texture first
            var texture = ReplantedOnlineMod.ModInfo.Assembly.LoadTextureFromResources(ResourcePath);
            if (texture == null)
            {
                Loadded = false;
                Failed = true;
                return;
            }

            texture.name = ResourcePath;

            // Calculate sprite dimensions
            int spriteWidth = texture.width / _columns;
            int spriteHeight = texture.height / _rows;

            var sprites = new List<Sprite>();

            // Extract each sprite from the grid
            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    // Calculate the pixel position of this sprite in the texture
                    Rect rect = new(
                        col * spriteWidth,
                        texture.height - (row + 1) * spriteHeight,
                        spriteWidth,
                        spriteHeight
                    );

                    // Create the sprite
                    var sprite = Sprite.Create(
                        texture,
                        rect,
                        new Vector2(0.5f, 0.5f),
                        _pixelsPerUnit
                    );

                    if (sprite != null)
                    {
                        sprite.name = $"{ResourcePath}_{row}_{col}";
                        sprites.Add(sprite);
                        sprite.hideFlags |= HideFlags.HideAndDontSave;
                    }
                }
            }

            if (sprites.Count > 0)
            {
                Loadded = true;
                Failed = false;
                Asset = [.. sprites];
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

    /// <summary>
    /// Gets a specific sprite from the loaded sprite sheet by row and column index.
    /// </summary>
    /// <param name="row">The row index (0-based).</param>
    /// <param name="column">The column index (0-based).</param>
    /// <returns>The sprite at the specified grid position.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the asset is not loaded.</exception>
    /// <exception cref="IndexOutOfRangeException">Thrown when the row or column is out of range.</exception>
    internal Sprite GetSprite(int row, int column)
    {
        if (!Loadded || Asset == null)
            throw new InvalidOperationException($"Asset not loaded: {ResourcePath}");

        int index = row * _columns + column;
        if (index < 0 || index >= Asset.Length)
            throw new IndexOutOfRangeException($"Sprite at position ({row}, {column}) does not exist in {ResourcePath}");

        return Asset[index];
    }

    /// <summary>
    /// Gets a specific sprite from the loaded sprite sheet by index.
    /// </summary>
    /// <param name="index">The index of the sprite (0-based, row-major order).</param>
    /// <returns>The sprite at the specified index.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the asset is not loaded.</exception>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is out of range.</exception>
    internal Sprite this[int index]
    {
        get
        {
            if (!Loadded || Asset == null)
                throw new InvalidOperationException($"Asset not loaded: {ResourcePath}");

            if (index < 0 || index >= Asset.Length)
                throw new IndexOutOfRangeException($"Index {index} out of range for asset {ResourcePath} with {Asset.Length} sprites");

            return Asset[index];
        }
    }

    /// <summary>
    /// Gets the total number of sprites in the sheet.
    /// </summary>
    /// <value>The total sprite count (<see cref="_rows"/> * <see cref="_columns"/>).</value>
    internal int TotalSprites => _rows * _columns;

    /// <summary>
    /// Gets the number of rows in the sprite sheet grid.
    /// </summary>
    internal int Rows => _rows;

    /// <summary>
    /// Gets the number of columns in the sprite sheet grid.
    /// </summary>
    internal int Columns => _columns;
}
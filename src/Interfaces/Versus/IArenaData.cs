using Il2CppReloaded.Data;
using UnityEngine;

namespace ReplantedOnline.Interfaces.Versus;

/// <summary>
/// Defines the contract for arena data in versus gamemode.
/// </summary>
internal interface IArenaData
{
    /// <summary>
    /// Gets the level entry data for this arena.
    /// </summary>
    /// <returns>The level entry data.</returns>
    LevelEntryData GetLevelEntryData();

    /// <summary>
    /// Gets the thumbnail for this arena.
    /// </summary>
    /// <returns>The sprite for the thumbnail.</returns>
    Sprite GetThumbnail()
    {
        return GetLevelEntryData().EntryThumbnail.Asset.Cast<Sprite>();
    }

    /// <summary>
    /// Sets up the versus arena for gameplay with the specified level data.
    /// </summary>
    /// <param name="versusLevelData">The level data to configure the arena with.</param>
    void SetupVersusArenaForGameplay(LevelEntryData versusLevelData);
}

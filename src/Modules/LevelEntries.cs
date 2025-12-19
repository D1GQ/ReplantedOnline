using Il2CppReloaded.Data;
using UnityEngine;

namespace ReplantedOnline.Modules;

/// <summary>
/// Manages and provides access to level data entries.
/// This class maintains a cache of all available levels for quick lookup by name.
/// </summary>
internal static class LevelEntries
{
    private static List<LevelEntryData> _allLevels = [];
    private static readonly Dictionary<string, LevelEntryData> _levelNameLookup = [];

    /// <summary>
    /// Initializes the level cache by finding all LevelEntryData objects in the game resources.
    /// This should be called early in the mod initialization process.
    /// </summary>
    internal static void Init()
    {
        var levels = Resources.FindObjectsOfTypeAll<LevelEntryData>();
        _allLevels = [.. levels];

        foreach (var level in _allLevels)
        {
            _levelNameLookup[level.name] = level;
        }
    }

    /// <summary>
    /// Retrieves a LevelEntryData object by its name.
    /// </summary>
    /// <param name="name">The name of the level to retrieve.</param>
    /// <returns>
    /// The LevelEntryData object if found; otherwise, returns the default value for LevelEntryData.
    /// </returns>
    internal static LevelEntryData GetLevel(string name)
    {
        if (_levelNameLookup.TryGetValue(name, out var levelData))
        {
            return levelData;
        }
        return default;
    }
}
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Utilities;
using System.Reflection;
using UnityEngine;

namespace ReplantedOnline.Modules.Versus.Arenas;

[RegisterArena]
internal sealed class RoofNightArena : RoofArena
{
    /// <inheritdoc/>
    public override ArenaTypes Type => ArenaTypes.RoofNight;

    /// <inheritdoc/>
    public override LevelEntryData GetLevelEntryData()
    {
        return LevelEntries.GetLevel("Level-AdventureArea5Level10");
    }

    /// <inheritdoc/>
    public override Sprite GetThumbnail()
    {
        return Assembly.GetExecutingAssembly().LoadSpriteFromResources("ReplantedOnline.Resources.Images.Arenas.Roofnight.png");
    }

    /// <inheritdoc/>
    public override void SetupVersusLevel(LevelEntryData versusLevelData)
    {
        versusLevelData.m_gameArea = GameArea.Boss;
        versusLevelData.m_backgroundPrefab = GetLevelEntryData().m_backgroundPrefab;
    }
}

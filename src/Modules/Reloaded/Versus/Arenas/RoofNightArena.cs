using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using UnityEngine;

namespace ReplantedOnline.Modules.Reloaded.Versus.Arenas;

[RegisterArena(ArenaType.RoofNight)]
internal sealed class RoofNightArena : RoofArena
{
    /// <inheritdoc/>
    public override ArenaType Type => ArenaType.RoofNight;

    /// <inheritdoc/>
    public override MusicTune Music => MusicTune.NightMoongrains;

    /// <inheritdoc/>
    public override LevelEntryData GetLevelEntryData()
    {
        return LevelEntries.GetLevel("Level-AdventureArea5Level10")!;
    }

    /// <inheritdoc/>
    public override Sprite GetThumbnail()
    {
        return ReplantedOnlineMod.Assets.Sprites.Arena.RoofNightThumbnail;
    }

    /// <inheritdoc/>
    public override void SetupVersusLevel(LevelEntryData versusLevelData)
    {
        versusLevelData.m_gameArea = GameArea.Boss;
        versusLevelData.m_backgroundPrefab = GetLevelEntryData().m_backgroundPrefab;
    }

    /// <inheritdoc/>
    public override void SetSeedPacketDefinition(PlantDefinition seedPacketDefinition)
    {
        seedPacketDefinition.m_versusCost = SeedPacketDefinitions.BaseSeedVersusCost[seedPacketDefinition.SeedType];

        if (Plant.IsNocturnal(seedPacketDefinition.SeedType))
        {
            // Add Cost of instant coffee to balance price
            seedPacketDefinition.m_versusCost += 25;
        }
    }
}

using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Instance;
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

    /// <inheritdoc/>
    public override void InitializeSeedPacketCooldowns(SeedPacket[] seedPackets)
    {
        foreach (var seedPacket in seedPackets)
        {
            if (seedPacket.mPacketType is SeedType.Sunflower or SeedType.Sunshroom or SeedType.ZombieGravestone) continue;

            seedPacket.Deactivate();
            var time = Instances.DataServiceActivity.Service.GetPlantDefinition(seedPacket.mPacketType)?.m_versusBaseRefreshTime ?? 0;
            // Start at least with a 15 second cooldown 
            seedPacket.mRefreshTime = Math.Max(time, ReplantedOnlineMod.Constants.NIGHT_SEEDPACKET_MIN_INITIAL_COOLDOWN);
            seedPacket.mRefreshing = true;
        }
    }
}

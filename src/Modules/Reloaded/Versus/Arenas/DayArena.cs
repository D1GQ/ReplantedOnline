using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Structs.Reloaded;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Modules.Reloaded.Versus.Arenas;

[RegisterArena]
internal class DayArena : IArena, IArenaData
{
    /// <inheritdoc/>
    public virtual ArenaTypes Type => ArenaTypes.Day;

    /// <inheritdoc/>
    public virtual MusicTune Music => MusicTune.MinigameLoonboon;

    /// <inheritdoc/>
    public SpawnType DefaultZombieSpawnType => SpawnType.RiseFromGround;

    /// <inheritdoc/>
    public virtual LevelEntryData GetLevelEntryData()
    {
        return LevelEntries.GetLevel("Level-AdventureArea1Level2")!;
    }

    /// <inheritdoc/>
    public virtual void SetupVersusLevel(LevelEntryData versusLevelData)
    {
        versusLevelData.m_gameArea = GameArea.Day;
        versusLevelData.m_backgroundPrefab = GetLevelEntryData().m_backgroundPrefab;
    }

    /// <inheritdoc/>
    public virtual CustomRecommentedFlags GetSeedTypeCustomRecommentedFlags(SeedType seedType)
    {
        if (seedType is SeedType.Umbrella or SeedType.Blover)
        {
            return CustomRecommentedFlags.Recommended | CustomRecommentedFlags.ExcludeFromRandom;
        }

        if (seedType == SeedType.Plantern)
        {
            return CustomRecommentedFlags.NotRecommended | CustomRecommentedFlags.ExcludeFromRandom;
        }

        if (Plant.IsAquatic(seedType) || seedType == CustomSeedType.DolphinRider || seedType == CustomSeedType.Snorkel)
        {
            return CustomRecommentedFlags.NotAllowed | CustomRecommentedFlags.ExcludeFromRandom;
        }

        if (seedType == SeedType.InstantCoffee)
        {
            return CustomRecommentedFlags.Recommended | CustomRecommentedFlags.ExcludeFromRandom;
        }

        if (Plant.IsNocturnal(seedType) && !PvZRUtils.IsSeedTypeInAnySeedBank(SeedType.InstantCoffee))
        {
            return CustomRecommentedFlags.NotRecommended;
        }

        return CustomRecommentedFlags.Recommended;
    }

    /// <inheritdoc/>
    public virtual void SetSeedPacketDefinition(PlantDefinition seedPacketDefinition)
    {
    }

    /// <inheritdoc/>
    public virtual void InitializeArena(VersusMode versusMode)
    {
        if (ReloadedLobby.AmLobbyHost())
        {
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 0, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 1, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 2, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 3, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 4, true);

            SeedPacketDefinitions.SpawnZombie(ZombieType.Gravestone, 8, 1, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Gravestone, 8, 3, true);

            SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, 0, 1, true);
            SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, 0, 3, true);
        }
    }

    /// <inheritdoc/>
    public void UpdateArena(VersusMode versusMode)
    {
        versusMode.m_board.mApp.BackgroundController.EnableBowlingLine(true, 515);
    }

    /// <inheritdoc/>
    public bool CanBePlacedAt(SeedType seedType, int gridX, int gridY) => true;
}

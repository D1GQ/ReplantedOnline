using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Client;
using static Il2CppReloaded.Gameplay.SeedChooserScreen;

namespace ReplantedOnline.Modules.Versus.Arenas;

[RegisterArena]
internal sealed class DayArena : IArena, ISetupSeedbank
{
    /// <inheritdoc/>
    public ArenaTypes Type => ArenaTypes.Day;

    /// <inheritdoc/>
    public void InitializeArena(VersusMode versusMode)
    {
        if (ReplantedLobby.AmLobbyHost())
        {
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 0, false, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 1, false, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 2, false, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 3, false, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 4, false, true);

            SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, SeedType.Sunflower, 0, 1, true);
            SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, SeedType.Sunflower, 0, 3, true);

            SeedPacketDefinitions.SpawnZombie(ZombieType.Gravestone, 8, 1, false, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Gravestone, 8, 3, false, true);
        }
    }

    /// <inheritdoc/>
    public void InitializeSeedPacketCooldowns(SeedPacket[] seedPackets)
    {
        foreach (var seedPacket in seedPackets)
        {
            if (SeedPacketDefinitions.IgnoreInitialCooldown.Contains(seedPacket.mPacketType)) continue;

            seedPacket.Deactivate();
            seedPacket.mRefreshTime = Instances.DataServiceActivity.Service.GetPlantDefinition(seedPacket.mPacketType)?.m_versusBaseRefreshTime ?? 0;
            seedPacket.mRefreshing = true;
        }
    }

    /// <inheritdoc/>
    public void SetupSeedbank(SeedBank seedBank, SeedBankInfo seedBankInfo, PlayerTeam team)
    {
        if (team == PlayerTeam.Plants)
        {
            seedBankInfo.mSeedsInBank = 1;
            seedBank.AddSeed(SeedType.Sunflower, true);
        }
        else
        {
            seedBankInfo.mSeedsInBank = 1;
            seedBank.AddSeed(SeedType.ZombieGravestone, true);
        }
    }

    /// <inheritdoc/>
    public void UpdateArena(VersusMode versusMode) { }

    /// <inheritdoc/>
    public bool CanBePlacedAt(SeedType seedType, int gridX, int gridY) => true;
}

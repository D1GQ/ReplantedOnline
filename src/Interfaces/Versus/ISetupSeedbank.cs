using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Utilities;
using static Il2CppReloaded.Gameplay.SeedChooserScreen;

namespace ReplantedOnline.Interfaces.Versus;

/// <summary>
/// Defines the contract for configuring seed banks for different teams in versus mode.
/// </summary>
internal interface ISetupSeedbank
{
    /// <summary>
    /// Gets the amount of seed packets that are available.
    /// </summary>
    int SeedPacketCount { get; }

    /// <summary>
    /// Gets the base amount of seed packets available.
    /// </summary>
    static internal int BaseSeedPacketCount => VersusMode.k_numPackets;

    /// <summary>
    /// Configures the seed bank for a specific team with initial seeds.
    /// </summary>
    /// <param name="seedBankInfo">The seed bank info instance to configure</param>
    /// <param name="team">The team whose seed bank is being configured</param>
    void SetupSeedbank(SeedBankInfo seedBankInfo, PlayerTeam team);

    /// <summary>
    /// Performs base configuration of the seed bank for a specific team.
    /// </summary>
    /// <param name="seedBankInfo">The seed bank info instance to configure</param>
    /// <param name="team">The team whose seed bank is being configured</param>
    internal static void BaseSetupSeedbank(SeedBankInfo seedBankInfo, PlayerTeam team)
    {
        if (team == PlayerTeam.Plants)
        {
            seedBankInfo.AddSeedFromChooser(SeedType.Sunflower);
        }
        else
        {
            seedBankInfo.AddSeedFromChooser(SeedType.ZombieGravestone);
        }
    }

    /// <summary>
    /// Configures the seed bank for a specific team on Quickplay.
    /// </summary>
    /// <param name="seedBankInfo">The seed bank info instance to configure</param>
    /// <param name="team">The team whose seed bank is being configured</param>
    void QuickPlayAddSeedsToSeedbank(SeedBankInfo seedBankInfo, PlayerTeam team);

    /// <summary>
    /// Performs base Quickplay seed bank configuration for a specific team.
    /// </summary>
    /// <param name="seedBankInfo">The seed bank info instance to configure</param>
    /// <param name="team">The team whose seed bank is being configured</param>
    internal static void BaseQuickPlayAddSeedsToSeedbank(SeedBankInfo seedBankInfo, PlayerTeam team)
    {
        if (team == PlayerTeam.Plants)
        {
            foreach (var seedType in Instances.GameplayActivity.VersusMode.m_quickPlayPlants)
            {
                seedBankInfo.mSeedBank.AddSeed(seedType, true);
            }
        }
        else if (team == PlayerTeam.Zombies)
        {
            foreach (var seedType in Instances.GameplayActivity.VersusMode.m_quickPlayZombies)
            {
                seedBankInfo.mSeedBank.AddSeed(seedType, true);
            }
        }
    }
}
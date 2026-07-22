using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Interfaces.Versus;

/// <summary>
/// Defines the contract for configuring seed banks for different teams in versus mode.
/// </summary>
internal interface IArenaSetupSeedbank
{
    /// <summary>
    /// Gets the amount of seed packets.
    /// </summary>
    int SeedPacketCount { get; }

    /// <summary>
    /// Gets the array of plant seed types for quick play selection.
    /// </summary>
    SeedType[] QuickPlayPlants { get; }

    /// <summary>
    /// Gets the array of zombie seed types for quick play selection.
    /// </summary>
    SeedType[] QuickPlayZombies { get; }

    /// <summary>
    /// Gets the seed packet count from the current arena or default versus mode.
    /// </summary>
    /// <returns>The number of seed packets available.</returns>
    internal static int GetSeedPacketCount()
    {
        if (IArena.GetCurrentArena() is IArenaSetupSeedbank setupSeedbank)
        {
            return setupSeedbank.SeedPacketCount;
        }
        else
        {
            return VersusMode.k_numPackets;
        }
    }

    /// <summary>
    /// Retrieves the quick play plants seed types from the current arena or default versus mode.
    /// </summary>
    /// <returns>An array of plant seed types for quick play.</returns>
    internal static SeedType[] GetQuickPlayPlants()
    {
        if (IArena.GetCurrentArena() is IArenaSetupSeedbank setupSeedbank)
        {
            return setupSeedbank.QuickPlayPlants;
        }
        else
        {
            return Instances.GameplayActivity.VersusMode.m_quickPlayPlants;
        }
    }

    /// <summary>
    /// Retrieves the quick play zombies seed types from the current arena or default versus mode.
    /// </summary>
    /// <returns>An array of zombie seed types for quick play.</returns>
    internal static SeedType[] GetQuickPlayZombies()
    {
        if (IArena.GetCurrentArena() is IArenaSetupSeedbank setupSeedbank)
        {
            return setupSeedbank.QuickPlayZombies;
        }
        else
        {
            return Instances.GameplayActivity.VersusMode.m_quickPlayZombies;
        }
    }

    /// <summary>
    /// Adds initial seeds to both local and opponent seed banks based on each player's team.
    /// Clears existing seeds from both banks before adding the starting seeds.
    /// </summary>
    internal static void AddInitialSeedsToBanks()
    {
        var plantSeedBankInfo = PvZRUtils.GetPlantSeedBankInfo();
        var zombieSeedBankInfo = PvZRUtils.GetZombieSeedBankInfo();
        plantSeedBankInfo.ClearAllSeedsInSeedBack();
        zombieSeedBankInfo.ClearAllSeedsInSeedBack();
    }
}
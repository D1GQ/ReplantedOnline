using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities;

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
    /// Gets the amount of starting seed packets.
    /// </summary>
    int StartingSeedPacketCount { get; }

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
    /// Gets the starting seed packet count from the current arena or default value.
    /// </summary>
    /// <returns>The number of starting seed packets (default is 1).</returns>
    internal static int GetStartingSeedPacketCount()
    {
        if (IArena.GetCurrentArena() is IArenaSetupSeedbank setupSeedbank)
        {
            return setupSeedbank.StartingSeedPacketCount;
        }
        else
        {
            return 1;
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
        var localSeedBankInfo = PvZRUtils.GetLocalSeedBankInfo();
        var opponentSeedBankInfo = PvZRUtils.GetOpponentSeedBankInfo();
        localSeedBankInfo.ClearAllSeedsInSeedBack();
        opponentSeedBankInfo.ClearAllSeedsInSeedBack();

        if (ReplantedClientData.LocalClient.Team == PlayerTeam.Plants)
        {
            for (int i = 0; i < GetStartingSeedPacketCount(); i++)
            {
                localSeedBankInfo.AddSeedFromChooser(GetQuickPlayPlants()[i]);
                opponentSeedBankInfo.AddSeedFromChooser(GetQuickPlayZombies()[i]);
            }
        }
        else
        {
            for (int i = 0; i < GetStartingSeedPacketCount(); i++)
            {
                localSeedBankInfo.AddSeedFromChooser(GetQuickPlayZombies()[i]);
                opponentSeedBankInfo.AddSeedFromChooser(GetQuickPlayPlants()[i]);
            }
        }
    }

    /// <summary>
    /// Determines whether the specified seed type should be excluded from random selection.
    /// </summary>
    /// <param name="seedType">The seed type to check.</param>
    /// <returns>True if the seed type matches any of the starting quick play plants or zombies; otherwise, false.</returns>
    internal static bool ExcludeSeedFromRandom(SeedType seedType)
    {
        for (int i = 0; i < GetStartingSeedPacketCount(); i++)
        {
            var plantSeedType = GetQuickPlayPlants()[i];
            var zombieSeedType = GetQuickPlayZombies()[i];
            if (seedType == plantSeedType || seedType == zombieSeedType)
            {
                return true;
            }
        }

        return false;
    }
}
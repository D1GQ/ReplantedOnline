using Il2CppReloaded.Gameplay;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Structs.Reloaded;

/// <summary>
/// Represents a set of seed types used for quickplay, exposing the seed packet count and the underlying seed type array.
/// </summary>
/// <param name="seedTypes">The seed types to populate into the seed bank.</param>
internal readonly struct QuickplaySeedTypes(SeedType[] seedTypes)
{
    /// <summary>
    /// The default quickplay seed types for the plant side.
    /// </summary>
    internal static readonly QuickplaySeedTypes DefaultPlantQuickplaySeedTypes = new([SeedType.Sunflower, SeedType.Peashooter, SeedType.Potatomine,
        SeedType.Wallnut, SeedType.Jalapeno, SeedType.Squash]);

    /// <summary>
    /// The default quickplay seed types for the zombie side.
    /// </summary>
    internal static readonly QuickplaySeedTypes DefaultZombieQuickplaySeedTypes = new([SeedType.ZombieGravestone, SeedType.ZombieNormal, SeedType.ZombieTrashCan,
                SeedType.ZombieTrafficCone, SeedType.ZombieFootball, SeedType.ZombieFlag]);

    /// <summary>
    /// The number of seed packets in this quickplay set.
    /// </summary>
    internal readonly int PacketCount = seedTypes.Length;

    /// <summary>
    /// The seed types included in this quickplay set.
    /// </summary>
    internal readonly SeedType[] SeedTypes = seedTypes;

    /// <summary>
    /// Clears the specified seed bank and repopulates it with this set's seed types.
    /// </summary>
    /// <param name="seedBankInfo">The seed bank information whose seed bank will be cleared and repopulated.</param>
    internal void SetSeedPacketsFromBank(SeedChooserScreen.SeedBankInfo seedBankInfo)
    {
        seedBankInfo.ClearAllSeedsInSeedBack();
        foreach (var seedType in SeedTypes)
        {
            seedBankInfo.mSeedBank.AddSeed(seedType, true);
        }
    }
}
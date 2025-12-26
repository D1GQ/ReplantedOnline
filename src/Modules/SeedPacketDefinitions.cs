using Il2CppReloaded.Gameplay;

namespace ReplantedOnline.Modules;

/// <summary>
/// Handles seed packet definition modifications including versus costs and other properties.
/// </summary>
internal static class SeedPacketDefinitions
{
    internal static SeedType[] DisabledSeedTypes = [
        // Misc
        SeedType.NumSeedsInChooser,
        SeedType.NumSeedTypes,
        SeedType.LastZombieIndex,
        SeedType.None,

        // Plants
        SeedType.Gravebuster,
        SeedType.Lilypad,
        SeedType.Tanglekelp,
        SeedType.Hypnoshroom,
        SeedType.Seashroom,
        SeedType.Plantern,
        SeedType.Blover,
        SeedType.Flowerpot,
        SeedType.Umbrella,
        SeedType.Marigold,

        // Zombies
        SeedType.ZombieDancer,
        SeedType.ZombiePolevaulter,
        SeedType.Zomboni,
        SeedType.ZombieJackInTheBox,
        SeedType.ZombieCatapult
    ];

    /// <summary>
    /// Initializes plant definitions and applies custom modifications.
    /// </summary>
    internal static void Initialize()
    {
        Instances.DataServiceActivity.Service.GetPlantDefinition(SeedType.ZombieFlag).m_versusCost = 250;
    }
}
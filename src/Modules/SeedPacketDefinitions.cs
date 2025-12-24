using Il2CppReloaded.Gameplay;

namespace ReplantedOnline.Modules;

/// <summary>
/// Handles seed packet definition modifications including versus costs and other properties.
/// </summary>
internal static class SeedPacketDefinitions
{
    internal static SeedType[] DisabledSeedTypes = [
        // Plants
        SeedType.Hypnoshroom,
        SeedType.Iceshroom,
        SeedType.Doomshroom,
        SeedType.Lilypad,
        SeedType.Seashroom,
        SeedType.Plantern,
        SeedType.Blover,
        SeedType.Flowerpot,
        SeedType.Umbrella,
        SeedType.Marigold,

        // Zombies
        SeedType.ZombiePolevaulter,
        SeedType.ZombieLadder,
        SeedType.ZombieDigger,
        SeedType.ZombieBungee,
        SeedType.Zomboni,
        SeedType.ZombiePogo,
        SeedType.ZombieJackInTheBox,
        SeedType.ZombieCatapult
        ];

    /// <summary>
    /// Initializes plant definitions and applies custom modifications.
    /// </summary>
    internal static void Initialize()
    {
        Instances.DataServiceActivity.Service.GetPlantDefinition(SeedType.ZombieFlag).m_versusCost = 225;
    }
}
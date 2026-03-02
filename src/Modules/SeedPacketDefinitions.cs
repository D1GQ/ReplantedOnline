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
        SeedType.Seashroom,
        SeedType.Blover,

        // Zombies
        // Needs to be fixed
        SeedType.Zomboni,
        SeedType.ZombieCatapult,
    ];

    internal static SeedType[] IgnoreInitialCooldown = [
        // Plants
        SeedType.Sunflower,
        SeedType.Peashooter,
        SeedType.Potatomine,
        SeedType.Wallnut,
        SeedType.GiantWallnut,
        SeedType.Puffshroom,

        // Zombies
        SeedType.ZombieGravestone,
        SeedType.ZombieNormal,
        SeedType.ZombieTrashCan
    ];

    /// <summary>
    /// Initializes plant definitions and applies custom modifications.
    /// </summary>
    internal static void Initialize()
    {
        Instances.DataServiceActivity.Service.GetPlantDefinition(SeedType.ZombieFlag).m_versusCost = 275;
    }
}
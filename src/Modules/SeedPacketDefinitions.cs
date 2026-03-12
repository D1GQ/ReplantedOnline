using Il2CppReloaded.Gameplay;
using ReplantedOnline.Modules.Instance;

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

    internal static SeedType[] ExcludeFromRandom = [
        // Plants
        SeedType.Sunflower,
        SeedType.Flowerpot,
        SeedType.Marigold,
        SeedType.Plantern,

        // Zombies
        SeedType.ZombieGravestone
    ];

    /// <summary>
    /// Initializes plant definitions and applies custom modifications.
    /// </summary>
    internal static void Initialize()
    {
        Instances.DataServiceActivity.Service.GetPlantDefinition(SeedType.ZombieFlag).m_versusCost = 275;
    }
}
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Structs.Reloaded;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Interfaces.Versus;

/// <summary>
/// Defines the contract for arena behavior in versus gamemode.
/// </summary>
internal interface IArena
{
    /// <summary>
    /// Gets the default spawn type for zombies in this arena.
    /// </summary>
    SpawnType DefaultZombieSpawnType { get; }

    /// <summary>
    /// Gets the custom recommended flags for a specific seed type.
    /// </summary>
    /// <param name="seedType">The seed type to get recommended flags for.</param>
    /// <returns>The custom recommended flags for the specified seed type.</returns>
    CustomRecommentedFlags GetSeedTypeCustomRecommentedFlags(SeedType seedType);

    /// <summary>
    /// Sets the seed packet definition for the arena.
    /// </summary>
    /// <param name="seedPacketDefinition">The plant definition to be used as the seed packet.</param>
    void SetSeedPacketDefinition(PlantDefinition seedPacketDefinition);

    /// <summary>
    /// Called when the versus gameplay starts.
    /// </summary>
    /// <param name="versusMode">The instance of VersusMode.</param>
    void InitializeArena(VersusMode versusMode);

    /// <summary>
    /// Called every frame during the versus game mode's active state.
    /// </summary>
    /// <param name="versusMode">The instance of VersusMode.</param>
    void UpdateArena(VersusMode versusMode);

    /// <summary>
    /// Determines whether the seed type can be placed at the specified grid coordinates in the given arena.
    /// </summary>
    /// <param name="seedType">The seed type being attempted to be placed</param>
    /// <param name="gridX">The X grid coordinate (column)</param>
    /// <param name="gridY">The Y grid coordinate (row)</param>
    /// <returns>True if the seed type can be placed at the specified location; otherwise, false</returns>
    bool CanBePlacedAt(SeedType seedType, int gridX, int gridY);

    public static CustomRecommentedFlags GetDefaultRecommentedFlags(SeedType seedType, ArenaType arenaType)
    {
        bool isNight = arenaType.IsArenaAtNight();
        bool isCloudy = arenaType == ArenaType.CloudyDay;
        bool isPool = arenaType is ArenaType.Pool or ArenaType.PoolNight;
        bool isFog = arenaType == ArenaType.PoolNight;
        bool hasNoDirt = arenaType is ArenaType.Roof or ArenaType.RoofNight or ArenaType.China;

        if (SeedPacketDefinitions.CurrencyProducingSeedTypes.Contains(seedType))
        {
            if (!Plant.IsNocturnal(seedType))
            {
                return CustomRecommentedFlags.Required | CustomRecommentedFlags.Recommended | CustomRecommentedFlags.ExcludeFromRandom;
            }
            else
            {
                if (!PvZRUtils.IsSeedTypeInAnySeedBank(SeedType.InstantCoffee) && !isNight && !isCloudy)
                {
                    return CustomRecommentedFlags.Required | CustomRecommentedFlags.NotRecommended | CustomRecommentedFlags.ExcludeFromRandom;
                }
                else
                {
                    return CustomRecommentedFlags.Required | CustomRecommentedFlags.Recommended | CustomRecommentedFlags.ExcludeFromRandom;
                }
            }
        }

        if (seedType is SeedType.Flowerpot or SeedType.Lilypad)
        {
            return CustomRecommentedFlags.NotAllowed | CustomRecommentedFlags.ExcludeFromRandom;
        }

        if (seedType == SeedType.Umbrella)
        {
            return CustomRecommentedFlags.Recommended | CustomRecommentedFlags.ExcludeFromRandom;
        }

        if (seedType == SeedType.Blover && !isFog)
        {
            return CustomRecommentedFlags.Recommended | CustomRecommentedFlags.ExcludeFromRandom;
        }

        if (seedType == SeedType.Plantern && !isFog)
        {
            return CustomRecommentedFlags.NotRecommended | CustomRecommentedFlags.ExcludeFromRandom;
        }

        if (seedType == SeedType.Flowerpot && !hasNoDirt)
        {
            return CustomRecommentedFlags.NotAllowed | CustomRecommentedFlags.ExcludeFromRandom;
        }

        if ((Plant.IsAquatic(seedType) || seedType == CustomSeedType.DolphinRider || seedType == CustomSeedType.Snorkel) && !isPool)
        {
            return CustomRecommentedFlags.NotAllowed | CustomRecommentedFlags.ExcludeFromRandom;
        }

        if (seedType == SeedType.InstantCoffee)
        {
            if (isCloudy)
            {
                return CustomRecommentedFlags.NotRecommended | CustomRecommentedFlags.ExcludeFromRandom;
            }

            if (!isNight)
            {
                return CustomRecommentedFlags.Recommended | CustomRecommentedFlags.ExcludeFromRandom;
            }
            else
            {
                return CustomRecommentedFlags.NotAllowed | CustomRecommentedFlags.ExcludeFromRandom;
            }
        }

        if (Plant.IsNocturnal(seedType) && !PvZRUtils.IsSeedTypeInAnySeedBank(SeedType.InstantCoffee) && !isNight && !isCloudy)
        {
            return CustomRecommentedFlags.NotRecommended;
        }

        return CustomRecommentedFlags.Recommended;
    }

    private static IArena? currentArenaCached;

    /// <summary>
    /// Captures the current arena instance from the registered arena lookup.
    /// </summary>
    internal static void CatchCurrentArena()
    {
        currentArenaCached = RegisterArena.GetInstanceFromLookup(VersusState.ArenaSynced)!;
    }

    /// <summary>
    /// Retrieves the current active arena instance.
    /// </summary>
    /// <returns>The currently active IArena implementation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no arena instance is cached.</exception>
    internal static IArena GetCurrentArena()
    {
        return currentArenaCached!;
    }
}
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
    /// <param name="boardUnitX">The board unit for the X axis.</param>
    /// <param name="boardUnitY">The board unit for the Y axis.</param>
    /// <returns>True if the seed type can be placed at the specified location; otherwise, false</returns>
    bool CanBePlacedAt(SeedType seedType, BoardUnitX boardUnitX, BoardUnitY boardUnitY);

    public static CustomRecommentedFlags GetDefaultRecommentedFlags(SeedType seedType, ArenaType arenaType)
    {
        bool isNight = arenaType.IsArenaAtNight();
        bool isCloudy = arenaType == ArenaType.CloudyDay;
        bool isPool = arenaType is ArenaType.Pool or ArenaType.PoolNight;
        bool isFog = arenaType == ArenaType.PoolNight;
        bool hasNoDirt = arenaType is ArenaType.Roof or ArenaType.RoofNight or ArenaType.China;

        if (Plant.IsUpgrade(seedType))
        {
            if (SeedPacketDefinitions.UpgradeToSeedTypeLookup.TryGetValue(seedType, out var requiredSeedType))
            {
                if (!PvZRUtils.IsSeedTypeInAnySeedBank(requiredSeedType))
                {
                    return CustomRecommentedFlags.NotRecommended | CustomRecommentedFlags.ExcludeFromRandom;
                }
            }
            else
            {
                return CustomRecommentedFlags.NotAllowed | CustomRecommentedFlags.ExcludeFromRandom;
            }
        }

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

        if ((Plant.IsAquatic(seedType) || seedType == CustomSeedType.ZombieDolphinRider || seedType == CustomSeedType.ZombieSnorkel) && !isPool)
        {
            return CustomRecommentedFlags.NotAllowed | CustomRecommentedFlags.ExcludeFromRandom;
        }

        if (seedType == SeedType.InstantCoffee)
        {
            if (isCloudy)
            {
                return CustomRecommentedFlags.NotRecommended | CustomRecommentedFlags.ExcludeFromRandom | CustomRecommentedFlags.ExcludeFromRandomDependency;
            }

            if (!isNight)
            {
                return CustomRecommentedFlags.Recommended | CustomRecommentedFlags.ExcludeFromRandom;
            }
            else
            {
                return CustomRecommentedFlags.NotAllowed | CustomRecommentedFlags.ExcludeFromRandom | CustomRecommentedFlags.ExcludeFromRandomDependency;
            }
        }

        if (Plant.IsNocturnal(seedType) && !PvZRUtils.IsSeedTypeInAnySeedBank(SeedType.InstantCoffee) && !isNight && !isCloudy)
        {
            return CustomRecommentedFlags.NotRecommended;
        }

        return CustomRecommentedFlags.Recommended;
    }

    /// <summary>
    /// Retrieves the current active arena instance.
    /// </summary>
    /// <returns>The currently active IArena implementation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no arena instance is cached.</exception>
    internal static IArena GetCurrentArena()
    {
        return RegisterArena.GetInstanceFromLookup(VersusState.ArenaSynced)!;
    }
}
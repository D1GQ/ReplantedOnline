using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Structs.Reloaded;
using ReplantedOnline.Utilities.Modded;
using UnityEngine;

namespace ReplantedOnline.Interfaces.Versus;

/// <summary>
/// Defines the contract for arena data in versus gamemode.
/// </summary>
internal interface IArenaData
{
    /// <summary>
    /// The MusicTune to play for this arena.
    /// </summary>
    MusicTune Music { get; }

    /// <summary>
    /// Gets the level entry data for this arena.
    /// </summary>
    /// <returns>The level entry data.</returns>
    LevelEntryData GetLevelEntryData();

    /// <summary>
    /// Gets the thumbnail for this arena.
    /// </summary>
    /// <returns>The sprite for the thumbnail.</returns>
    Sprite GetThumbnail()
    {
        return GetLevelEntryData().EntryThumbnail.Asset.Cast<Sprite>();
    }

    public static CustomRecommentedFlags GetDefaultRecommentedFlags(SeedType seedType, ArenaTypes arenaType)
    {
        bool isNight = arenaType.IsArenaAtNight();
        bool isCloudy = arenaType == ArenaTypes.CloudyDay;
        bool isPool = arenaType is ArenaTypes.Pool or ArenaTypes.PoolNight;
        bool isFog = arenaType == ArenaTypes.PoolNight;
        bool hasNoDirt = arenaType is ArenaTypes.Roof or ArenaTypes.RoofNight or ArenaTypes.China;

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

    /// <summary>
    /// Sets up the versus arena for gameplay with the specified level data.
    /// </summary>
    /// <param name="versusLevelData">The level data to configure the arena with.</param>
    void SetupVersusLevel(LevelEntryData versusLevelData);
}

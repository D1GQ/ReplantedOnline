using Il2CppReloaded.Gameplay;
using Il2CppSource.Utils;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Patches.Reloaded.Gameplay.UI;
using ReplantedOnline.Structs.Reloaded;
using ReplantedOnline.Utilities.Modded;
using System.Collections.ObjectModel;

namespace ReplantedOnline.Modules.Reloaded.Versus.Gamemodes;

/// <summary>
/// Versus Gamemode that has random seed and zombie packets.
/// </summary>
[RegisterVersusGameMode]
internal sealed class RandomGamemode : IVersusGamemode
{
    /// <summary>
    /// Defines seed type dependencies for the plant side based on the arena type, one random value seed will be added.
    /// </summary>
    internal static readonly Dictionary<ArenaTypes[], SeedType[]> StartingPlantDependencies = new()
    {
        { [ArenaTypes.Day, ArenaTypes.Night, ArenaTypes.Pool, ArenaTypes.PoolNight, ArenaTypes.China], [SeedType.Peashooter, SeedType.Repeater, SeedType.Snowpea, SeedType.Cabbagepult] },
        { [ArenaTypes.Roof, ArenaTypes.RoofNight], [SeedType.Cabbagepult, SeedType.Kernelpult] }
    };

    /// <summary>
    /// Defines seed type dependencies for the plant side. If any seeds in the key array are present, one random value seed will be added.
    /// </summary>
    internal static readonly Dictionary<SeedType[], SeedType[]> PlantSeedTypeDependencies = new()
    {
        { SeedPacketDefinitions.SleepingPlants, [SeedType.InstantCoffee] },
        { [SeedType.Zomboni], [SeedType.Spikeweed, SeedType.Squash, SeedType.Jalapeno] },
        { [SeedType.ZombieGargantuar], [SeedType.Cherrybomb, SeedType.Jalapeno, SeedType.Squash, SeedType.Tanglekelp] },
        { [SeedType.ZombiePail, SeedType.ZombieScreenDoor, SeedType.ZombieFootball], [SeedType.Wallnut] },
        { [SeedType.ZombieBungee], [SeedType.Umbrella] },
        { [SeedType.ZombieCatapult], [SeedType.Spikeweed, SeedType.Squash, SeedType.Umbrella] },
        { [SeedType.ZombieBalloon], [SeedType.Cactus, SeedType.Blover] },
        { [SeedType.ZombieDigger], [SeedType.Splitpea, SeedType.Starfruit] },
        { [CustomSeedType.Snorkel, CustomSeedType.DolphinRider], [SeedType.Tanglekelp] }
    };

    /// <summary>
    /// The plant seed types that have been chosen for the current random gamemode in StartGameRpc.cs
    /// </summary>
    internal static List<SeedType> ChosenPlantSeedTypes = [];

    /// <summary>
    /// The zombie seed types that have been chosen for the current random gamemode in StartGameRpc.cs
    /// </summary>
    internal static List<SeedType> ChosenZombiesSeedTypes = [];

    /// <inheritdoc/>
    public void OnGameModeStart(VersusMode versusMode)
    {
        VersusLobbyPatch.VsSideChooser?.gameObject?.SetActive(false);
        versusMode.Phase = VersusPhase.Gameplay;
        StateTransitionUtils.Transition("InGame");
    }

    /// <inheritdoc/>
    public void OnGameplayStart(VersusMode versusMode)
    {
        IArenaSetupSeedbank.AddInitialSeedsToBanks();

        foreach (var seedType in ChosenPlantSeedTypes)
        {
            PvZRUtils.GetPlantSeedBankInfo().mSeedBank.AddSeed(seedType, true);
        }

        foreach (var seedType in ChosenZombiesSeedTypes)
        {
            PvZRUtils.GetZombieSeedBankInfo().mSeedBank.AddSeed(seedType, true);
        }

        // Set opponent seeds to hide, which will be revealed once SyncSeedPacketRpc.cs is received
        for (int i = 0; i < Instances.GameplayActivity.Board.SeedBanks.OpponentItem().SeedPackets.Count; i++)
        {
            SeedPacket seedPacket = Instances.GameplayActivity.Board.SeedBanks.OpponentItem().SeedPackets[i];
            seedPacket.mActive = false;
            seedPacket.PacketType = SeedPacketDefinitions.RandomHiddenSeed;
        }
    }

    /// <inheritdoc/>
    public void UpdateGameplay(VersusMode versusMode) { }

    /// <inheritdoc/>
    public void OnGameplayEnd(VersusMode versusMode, PlayerTeam winningTeam) { }

    /// <summary>
    /// Randomly selects zombie seed packets for the arena.
    /// </summary>
    /// <returns>A list of randomly selected zombie seed types.</returns>
    internal static List<SeedType> PickZombieSeedPacketTypes()
    {
        List<SeedType> zombieSeedTypes = [];

        int numSeedsToAdd = IArenaSetupSeedbank.GetSeedPacketCount();
        for (int i = 0; i < numSeedsToAdd; i++)
        {
            if (TryAddRequiredSeedType(zombieSeedTypes, true))
                continue;

            SeedType seedType = GetRandomSeedType(zombieSeedTypes.AsReadOnly(), true, false);
            zombieSeedTypes.Add(seedType);
        }

        return zombieSeedTypes;
    }

    /// <summary>
    /// Randomly selects plant seed packets based on the zombie seeds present.
    /// </summary>
    /// <param name="zombieSeedTypes">The zombie seeds already selected.</param>
    /// <returns>A list of randomly selected plant seed types.</returns>
    internal static List<SeedType> PickPlantSeedPacketTypes(ReadOnlyCollection<SeedType> zombieSeedTypes)
    {
        List<SeedType> plantSeedTypes = [];

        int numSeedsToAdd = IArenaSetupSeedbank.GetSeedPacketCount();
        for (int i = 0; i < numSeedsToAdd; i++)
        {
            if (TryAddRequiredSeedType(plantSeedTypes, false))
                continue;

            if (TryAddStartingPlantSeedType(plantSeedTypes, VersusState.Arena))
                continue;

            if (TryAddDependentPlantSeedType(plantSeedTypes, zombieSeedTypes))
                continue;

            SeedType seedType = GetRandomSeedType(plantSeedTypes.AsReadOnly(), false, i == numSeedsToAdd - 1);
            plantSeedTypes.Add(seedType);
        }

        return plantSeedTypes;
    }

    /// <summary>
    /// Gets a random seed type that passes all validation checks.
    /// </summary>
    /// <param name="currentSeedTypes">Seeds already selected.</param>
    /// <param name="zombieSeedTypes">True for zombie seeds, false for plant seeds.</param>
    /// <param name="excludeDependentSeedTypes">True to exclude seeds that are defined as dependencies in PlantSeedTypeDependencies.</param>
    /// <returns>A valid random seed type.</returns>
    /// <exception cref="Exception">Thrown when no valid seed type is found.</exception>
    private static SeedType GetRandomSeedType(ReadOnlyCollection<SeedType> currentSeedTypes, bool zombieSeedTypes, bool excludeDependentSeedTypes)
    {
        var arena = IArena.GetCurrentArena();

        List<SeedType> customSeedTypes = [.. CustomSeedType.CustomSeedTypes.Select(s => (SeedType)s)];
        List<SeedType> shuffledSeedTypes = [.. Enum.GetValues<SeedType>().Concat(customSeedTypes).Shuffle()];

        foreach (var seedType in shuffledSeedTypes)
        {
            if (excludeDependentSeedTypes)
            {
                bool hasDependency = false;
                foreach (var (hasDeps, deps) in PlantSeedTypeDependencies)
                {
                    if (hasDeps.Contains(seedType) && !deps.Any(s => !currentSeedTypes.Contains(s) &&
                    arena.GetSeedTypeCustomRecommentedFlags(s) !=
                    CustomRecommentedFlags.ExcludeFromRandomDependency))
                    {
                        hasDependency = true;
                        break;
                    }
                }

                if (hasDependency)
                    continue;
            }

            if (currentSeedTypes.Contains(seedType))
                continue;

            if (Challenge.IsZombieSeedType(seedType) != zombieSeedTypes)
                continue;

            if (Plant.IsUpgrade(seedType))
                continue;

            if (arena.GetSeedTypeCustomRecommentedFlags(seedType)
                .HasFlag(CustomRecommentedFlags.ExcludeFromRandom))
                continue;

            if (SeedPacketDefinitions.NoneSeedTypes.Contains(seedType))
                continue;

            if (SeedPacketDefinitions.HideInChooserSeedTypes.Contains(seedType))
                continue;

            if (SeedPacketDefinitions.CurrencyProducingSeedTypes.Contains(seedType))
                continue;

            var plantDef = Instances.IDataService.GetPlantDefinition(seedType);
            if (plantDef == null || plantDef.VersusCost == 0)
                continue;

            return seedType;
        }

        throw new Exception("No valid seed type found.");
    }

    /// <summary>
    /// Attempts to add a required seed type to the seed list based on arena requirements and currency-producing seed availability.
    /// </summary>
    /// <param name="seedTypes">The current list of seed types. If a required seed is found, it will be added to this list.</param>
    /// <param name="zombieSeedTypes">If true, checks for required zombie seed types; if false, checks for required plant seed types.</param>
    /// <returns>True if a required seed type was successfully added; otherwise, false.</returns>
    private static bool TryAddRequiredSeedType(List<SeedType> seedTypes, bool zombieSeedTypes)
    {
        var arena = IArena.GetCurrentArena();

        List<SeedType> customSeedTypes = [.. CustomSeedType.CustomSeedTypes.Select(s => (SeedType)s)];
        List<SeedType> shuffledSeedTypes = [.. Enum.GetValues<SeedType>().Concat(customSeedTypes).Shuffle()];
        List<SeedType> availableSeedTypes = [];

        foreach (var seedType in shuffledSeedTypes)
        {
            if (seedTypes.Contains(seedType))
                continue;

            if (Challenge.IsZombieSeedType(seedType) != zombieSeedTypes)
                continue;

            if (!arena.GetSeedTypeCustomRecommentedFlags(seedType)
                .HasFlag(CustomRecommentedFlags.Required))
                continue;

            if (SeedPacketDefinitions.CurrencyProducingSeedTypes.Contains(seedType))
            {
                if (SeedPacketDefinitions.CurrencyProducingSeedTypes.Any(seedTypes.Contains))
                    continue;
            }

            availableSeedTypes.Add(seedType);
        }

        if (availableSeedTypes.Count > 0)
        {
            seedTypes.Add(availableSeedTypes.Shuffle().First());
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks for arena-based seed dependencies and adds a random dependent seed to the plant list if conditions are met.
    /// </summary>
    /// <param name="plantSeedTypes">Current plant seeds. Will be modified if dependencies are found.</param>
    /// <param name="arenaType">The current arena type.</param>
    /// <returns>True if a dependent seed was added, otherwise false.</returns>
    private static bool TryAddStartingPlantSeedType(List<SeedType> plantSeedTypes, ArenaTypes arenaType)
    {
        foreach (var (arenaTypes, dependentSeeds) in StartingPlantDependencies)
        {
            if (!arenaTypes.Contains(arenaType))
                continue;

            if (dependentSeeds.Any(plantSeedTypes.Contains))
                continue;

            var shuffled = dependentSeeds.Shuffle();
            plantSeedTypes.Add(shuffled.First());
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks for seed dependencies and adds a random dependent seed to the plant list if conditions are met.
    /// </summary>
    /// <param name="plantSeedTypes">Current plant seeds. Will be modified if dependencies are found.</param>
    /// <param name="zombieSeedTypes">The zombie seeds already selected.</param>
    /// <returns>True if a dependent seed was added, otherwise false.</returns>
    private static bool TryAddDependentPlantSeedType(List<SeedType> plantSeedTypes, ReadOnlyCollection<SeedType> zombieSeedTypes)
    {
        var arena = IArena.GetCurrentArena();

        foreach (var (hasDeps, deps) in PlantSeedTypeDependencies)
        {
            if (!hasDeps.Any(plantSeedTypes.Contains) && !hasDeps.Any(zombieSeedTypes.Contains))
                continue;

            if (deps.Any(plantSeedTypes.Contains))
                continue;

            List<SeedType> availableDependents = [];
            foreach (var dependent in deps)
            {
                var flags = arena.GetSeedTypeCustomRecommentedFlags(dependent);
                if (flags.HasFlag(CustomRecommentedFlags.ExcludeFromRandomDependency))
                    continue;

                if (flags.HasFlag(CustomRecommentedFlags.NotAllowed))
                    continue;

                availableDependents.Add(dependent);
            }

            if (availableDependents.Count > 0)
            {
                plantSeedTypes.Add(availableDependents.Shuffle().First());
                return true;
            }
        }

        return false;
    }
}
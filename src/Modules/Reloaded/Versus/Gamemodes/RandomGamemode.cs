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

        if (VersusState.AmPlantSide)
        {
            foreach (var seedType in ChosenPlantSeedTypes)
            {
                versusMode.m_board.SeedBanks.LocalItem().AddSeed(seedType, true);
            }
        }
        else if (VersusState.AmZombieSide)
        {
            foreach (var seedType in ChosenZombiesSeedTypes)
            {
                versusMode.m_board.SeedBanks.LocalItem().AddSeed(seedType, true);
            }
        }

        // Set opponent seeds to hide, which will be revealed once SyncSeedPacketRpc.cs is received
        for (int i = IArenaSetupSeedbank.GetStartingSeedPacketCount(); i < Instances.GameplayActivity.Board.SeedBanks.OpponentItem().SeedPackets.Count; i++)
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

        int numSeedsToAdd = Instances.GameplayActivity.Board.SeedBanks.LocalItem().NumPackets -
            Instances.GameplayActivity.Board.SeedBanks.LocalItem().GetPacketCount();

        for (int i = 0; i < numSeedsToAdd; i++)
        {
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
        int numSeedsToAdd = Instances.GameplayActivity.Board.SeedBanks.LocalItem().NumPackets -
            Instances.GameplayActivity.Board.SeedBanks.LocalItem().GetPacketCount();

        for (int i = 0; i < numSeedsToAdd; i++)
        {
            if (TryAddStartingPlantSeedType(plantSeedTypes, VersusState.Arena))
            {
                continue;
            }

            if (TryAddDependentPlantSeedType(plantSeedTypes, zombieSeedTypes))
            {
                continue;
            }

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
        var shuffledSeedTypes = Enum.GetValues<SeedType>().Shuffle().ToList();

        foreach (var seedType in shuffledSeedTypes)
        {
            if (excludeDependentSeedTypes)
            {
                bool isDependent = false;
                foreach (var dependencies in PlantSeedTypeDependencies)
                {
                    if (dependencies.Key.Contains(seedType))
                    {
                        foreach (var dep in dependencies.Value)
                        {
                            if (!ShouldExcludeDependency(dep))
                            {
                                isDependent = true;
                                break;
                            }
                        }

                        if (isDependent)
                        {
                            break;
                        }
                    }
                }

                if (isDependent)
                {
                    continue;
                }
            }

            if (currentSeedTypes.Contains(seedType))
                continue;

            if (Challenge.IsZombieSeedType(seedType) != zombieSeedTypes)
                continue;

            if (!ICharacterConfig.IsAllowedInArena(seedType, VersusState.Arena))
                continue;

            if (IArena.GetCurrentArena() is IArenaSetupSeedbank arenaSetupSeedbank &&
                !arenaSetupSeedbank.IsSeedTypeAllowedInRandomGamemode(seedType))
                continue;

            if (IArenaSetupSeedbank.ExcludeSeedFromRandom(seedType))
                continue;

            if (SeedPacketDefinitions.NoneSeedTypes.Contains(seedType))
                continue;

            if (SeedPacketDefinitions.ExcludeFromRandomSeedTypes.Contains(seedType))
                continue;

            if (Instances.IDataService.GetPlantDefinition(seedType).VersusCost == 0)
                continue;

            return seedType;
        }

        throw new Exception("No valid seed type found.");
    }

    /// <summary>
    /// Checks for arena-based seed dependencies and adds a random dependent seed to the plant list if conditions are met.
    /// </summary>
    /// <param name="plantSeedTypes">Current plant seeds. Will be modified if dependencies are found.</param>
    /// <param name="arenaType">The current arena type.</param>
    /// <returns>True if a dependent seed was added, otherwise false.</returns>
    private static bool TryAddStartingPlantSeedType(List<SeedType> plantSeedTypes, ArenaTypes arenaType)
    {
        foreach (var dependency in StartingPlantDependencies)
        {
            bool arenaMatch = false;

            foreach (var arena in dependency.Key)
            {
                if (arena == arenaType)
                {
                    arenaMatch = true;
                    break;
                }
            }

            if (!arenaMatch)
                continue;

            bool hasDependent = false;
            foreach (var dependentSeed in dependency.Value)
            {
                if (plantSeedTypes.Contains(dependentSeed))
                {
                    hasDependent = true;
                    break;
                }
            }

            if (hasDependent)
                continue;

            var shuffled = dependency.Value.Shuffle();
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
        foreach (var dependency in PlantSeedTypeDependencies)
        {
            bool requiredSeedsPresent = false;
            foreach (var needed in dependency.Key)
            {
                if (plantSeedTypes.Contains(needed) || zombieSeedTypes.Contains(needed))
                {
                    requiredSeedsPresent = true;
                    break;
                }
            }

            if (!requiredSeedsPresent)
                continue;

            var availableDependents = new List<SeedType>();
            foreach (var dependentSeed in dependency.Value)
            {
                if (!plantSeedTypes.Contains(dependentSeed) && !ShouldExcludeDependency(dependentSeed))
                {
                    availableDependents.Add(dependentSeed);
                }
            }

            if (availableDependents.Count > 0)
            {
                var shuffled = availableDependents.Shuffle();
                plantSeedTypes.Add(shuffled.First());
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether a seed type should be excluded as a dependency.
    /// </summary>
    /// <param name="seedType">The seed type to check.</param>
    /// <returns>True if the seed type should be excluded; otherwise, false.</returns>
    private static bool ShouldExcludeDependency(SeedType seedType)
    {
        if (IArena.GetCurrentArena() is IArenaSetupSeedbank arenaSetupSeedbank &&
            !arenaSetupSeedbank.IsSeedTypeAllowedInRandomGamemode(seedType))
            return true;

        if (!ICharacterConfig.IsAllowedInArena(seedType, VersusState.Arena))
            return true;

        return false;
    }
}

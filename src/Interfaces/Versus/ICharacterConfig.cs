using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities.Il2cpp;

namespace ReplantedOnline.Interfaces.Versus;

/// <summary>
/// Base marker interface for all character configuration types.
/// Provides a non-generic foundation for the character configuration.
/// </summary>
internal interface ICharacterConfig
{
    /// <summary>
    /// Called when a plant is placed on the board. Routes the event to the appropriate plant configuration.
    /// </summary>
    /// <param name="plant">The plant instance that was planted</param>
    /// <param name="gridX">The X grid coordinate (column).</param>
    /// <param name="gridY">The Y grid coordinate (row).</param>
    internal static void OnPlantPlanted(Plant plant, int gridX, int gridY)
    {
        if (!ReloadedLobby.AmInLobby()) return;

        if (RegisterPlantConfig.TryGetInstanceFromLookup(plant.mSeedType, out var config))
        {
            config.OnPlanted(plant, gridX, gridY);
        }
    }

    /// <summary>
    /// Called when a zombie is placed on the board. Routes the event to the appropriate zombie configuration.
    /// </summary>
    /// <param name="zombie">The zombie instance that was planted</param>
    /// <param name="gridX">The X grid coordinate (column).</param>
    /// <param name="gridY">The Y grid coordinate (row).</param>
    internal static void OnZombiePlanted(Zombie zombie, int gridX, int gridY)
    {
        if (!ReloadedLobby.AmInLobby()) return;

        if (RegisterZombieConfig.TryGetInstanceFromLookup(zombie.mZombieType, out var config))
        {
            config.OnPlanted(zombie, gridX, gridY);
        }
    }

    /// <summary>
    /// Sets up arena definitions for all registered character configurations.
    /// Iterates through all zombie and plant definitions and applies them to matching configurations.
    /// </summary>
    internal static void SetArenaDefinitions(ArenaTypes arena)
    {
        foreach (var plantDefinition in Instances.IDataService.PlantDefinitions.EnumerateIl2CppReadonlyList())
        {
            if (RegisterPlantConfig.TryGetInstanceFromLookup(plantDefinition.SeedType, out var config))
            {
                config.SetArenaDefinition(plantDefinition, arena);
            }
        }

        foreach (var zombieDefinition in Instances.IDataService.ZombieDefinitions.EnumerateIl2CppReadonlyList())
        {
            if (RegisterZombieConfig.TryGetInstanceFromLookup(zombieDefinition.ZombieType, out var config))
            {
                config.SetArenaDefinition(zombieDefinition, arena);
            }
        }
    }

    /// <summary>
    /// Determines whether a seed type can be placed at the given grid coordinates in the specified arena.
    /// Checks both zombie and plant configurations based on the seed type.
    /// </summary>
    /// <param name="seedType">The seed type to check placement for</param>
    /// <param name="arena">The arena type where placement is being attempted</param>
    /// <param name="gridX">The X grid coordinate (column)</param>
    /// <param name="gridY">The Y grid coordinate (row)</param>
    /// <returns>True if the seed can be placed at the specified location; false if any matching configuration disallows placement</returns>
    internal static bool CanBePlacedAt(SeedType seedType, ArenaTypes arena, int gridX, int gridY)
    {
        if (!Challenge.IsZombieSeedType(seedType))
        {
            if (RegisterPlantConfig.TryGetInstanceFromLookup(seedType, out var config))
            {
                if (!config.CanBePlacedAt(arena, gridX, gridY))
                {
                    return false;
                }
            }
        }
        else
        {
            var zombieType = Challenge.IZombieSeedTypeToZombieType(seedType);
            if (RegisterZombieConfig.TryGetInstanceFromLookup(zombieType, out var config))
            {
                if (!config.CanBePlacedAt(arena, gridX, gridY))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Gets rather if a seedtype is allowd to be used in the arena.
    /// </summary>
    /// <param name="seedType">The seed type to check placement for</param>
    /// <param name="arena">The arena type where placement is being attempted</param>
    /// <returns>True if the seed can be used in the arena</returns>
    internal static bool IsAllowedInArena(SeedType seedType, ArenaTypes arena)
    {
        if (!Challenge.IsZombieSeedType(seedType))
        {
            if (RegisterPlantConfig.TryGetInstanceFromLookup(seedType, out var config))
            {
                if (!config.IsAllowedInArena(arena))
                {
                    return false;
                }
            }
        }
        else
        {
            var zombieType = Challenge.IZombieSeedTypeToZombieType(seedType);
            if (RegisterZombieConfig.TryGetInstanceFromLookup(zombieType, out var config))
            {
                if (!config.IsAllowedInArena(arena))
                {
                    return false;
                }
            }
        }

        return true;
    }
}

/// <summary>
/// Defines the configuration interface for character types in the game.
/// </summary>
/// <typeparam name="DefinitionType">The definition type containing character data (e.g., ZombieDefinition, PlantDefinition)</typeparam>
/// <typeparam name="CharacterType">The runtime character type (e.g., Zombie, Plant)</typeparam>
internal interface ICharacterConfig<DefinitionType, CharacterType> : ICharacterConfig
{
    /// <summary>
    /// Gets rather if the character definition is allowd to be used in the arena.
    /// </summary>
    /// <param name="arena">The current arena type</param>
    bool IsAllowedInArena(ArenaTypes arena);

    /// <summary>
    /// Configures the character definition with initial values and properties for arena.
    /// </summary>
    /// <param name="definition">The definition object to configure</param>
    /// <param name="arena">The current arena type</param>
    void SetArenaDefinition(DefinitionType definition, ArenaTypes arena);

    /// <summary>
    /// Determines whether the character can be placed at the specified grid coordinates in the given arena.
    /// </summary>
    /// <param name="arena">The arena type where placement is being attempted</param>
    /// <param name="gridX">The X grid coordinate (column)</param>
    /// <param name="gridY">The Y grid coordinate (row)</param>
    /// <returns>True if the character can be placed at the specified location; otherwise, false</returns>
    bool CanBePlacedAt(ArenaTypes arena, int gridX, int gridY);

    /// <summary>
    /// Called when a character is planted on the board.
    /// </summary>
    /// <param name="character">The character instance that was planted</param>
    /// <param name="gridX">The X grid coordinate (column)</param>
    /// <param name="gridY">The Y grid coordinate (row)</param>
    void OnPlanted(CharacterType character, int gridX, int gridY);
}

/// <summary>
/// Configuration interface specifically for zombies.
/// Implements the generic character configuration with zombie-specific types.
/// </summary>
internal interface IZombieConfig : ICharacterConfig<ZombieDefinition, Zombie> { }

/// <summary>
/// Configuration interface specifically for plants.
/// Implements the generic character configuration with plant-specific types.
/// </summary>
internal interface IPlantConfig : ICharacterConfig<PlantDefinition, Plant> { }
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Structs.Reloaded;

namespace ReplantedOnline.Interfaces.Versus;

/// <summary>
/// Base marker interface for all character logic types.
/// </summary>
internal interface ICharacterLogic
{
    /// <summary>
    /// Called when a plant is placed on the board. Routes the event to the appropriate plant logic.
    /// </summary>
    /// <param name="plant">The plant instance that was planted</param>
    /// <param name="boardUnitX">The board unit for the X axis.</param>
    /// <param name="boardUnitY">The board unit for the Y axis.</param>
    internal static void OnPlantPlanted(Plant plant, BoardUnitX boardUnitX, BoardUnitY boardUnitY)
    {
        if (!ReloadedLobby.AmInLobby())
            return;

        if (RegisterPlantLogic.TryGetInstanceFromLookup(plant.mSeedType, out var config))
        {
            config.OnPlanted(plant, boardUnitX, boardUnitY);
        }
    }

    /// <summary>
    /// Called when a zombie is placed on the board. Routes the event to the appropriate zombie logic.
    /// </summary>
    /// <param name="zombie">The zombie instance that was planted</param>
    /// <param name="boardUnitX">The board unit for the X axis.</param>
    /// <param name="boardUnitY">The board unit for the Y axis.</param>
    internal static void OnZombiePlanted(Zombie zombie, BoardUnitX boardUnitX, BoardUnitY boardUnitY)
    {
        if (!ReloadedLobby.AmInLobby())
            return;

        if (RegisterZombieLogic.TryGetInstanceFromLookup(zombie.mZombieType, out var config))
        {
            config.OnPlanted(zombie, boardUnitX, boardUnitY);
        }
    }

    /// <summary>
    /// Determines whether a seed type can be placed at the given grid coordinates in the specified arena.
    /// Checks both zombie and plant logic types based on the seed type.
    /// </summary>
    /// <param name="seedType">The seed type to check placement for</param>
    /// <param name="arena">The arena type where placement is being attempted</param>
    /// <param name="boardUnitX">The board unit for the X axis.</param>
    /// <param name="boardUnitY">The board unit for the Y axis.</param>
    /// <returns>True if the seed can be placed at the specified location; false if any matching logic disallows placement</returns>
    internal static bool CanBePlacedAt(SeedType seedType, ArenaType arena, BoardUnitX boardUnitX, BoardUnitY boardUnitY)
    {
        if (!Challenge.IsZombieSeedType(seedType))
        {
            if (RegisterPlantLogic.TryGetInstanceFromLookup(seedType, out var config))
            {
                if (!config.CanBePlacedAt(arena, boardUnitX, boardUnitY))
                {
                    return false;
                }
            }
        }
        else
        {
            var zombieType = Challenge.IZombieSeedTypeToZombieType(seedType);
            if (RegisterZombieLogic.TryGetInstanceFromLookup(zombieType, out var config))
            {
                if (!config.CanBePlacedAt(arena, boardUnitX, boardUnitY))
                {
                    return false;
                }
            }
        }

        return true;
    }
}

/// <summary>
/// Defines the logic interface for character types in the game.
/// </summary>
/// <typeparam name="DefinitionType">The definition type containing character data (e.g., ZombieDefinition, PlantDefinition)</typeparam>
/// <typeparam name="CharacterType">The runtime character type (e.g., Zombie, Plant)</typeparam>
internal interface ICharacterLogic<DefinitionType, CharacterType> : ICharacterLogic
{
    /// <summary>
    /// Determines whether the character can be placed at the specified grid coordinates in the given arena.
    /// </summary>
    /// <param name="arena">The arena type where placement is being attempted</param>
    /// <param name="boardUnitX">The board unit for the X axis.</param>
    /// <param name="boardUnitY">The board unit for the Y axis.</param>
    /// <returns>True if the character can be placed at the specified location; otherwise, false</returns>
    bool CanBePlacedAt(ArenaType arena, BoardUnitX boardUnitX, BoardUnitY boardUnitY);

    /// <summary>
    /// Called when a character is planted on the board.
    /// </summary>
    /// <param name="character">The character instance that was planted</param>
    /// <param name="boardUnitX">The board unit for the X axis.</param>
    /// <param name="boardUnitY">The board unit for the Y axis.</param>
    void OnPlanted(CharacterType character, BoardUnitX boardUnitX, BoardUnitY boardUnitY);
}

/// <summary>
/// Logic interface specifically for zombies.
/// Implements the generic character logic with zombie-specific types.
/// </summary>
internal interface IZombieLogic : ICharacterLogic<ZombieDefinition, Zombie>;

/// <summary>
/// Logic interface specifically for plants.
/// Implements the generic character logic with plant-specific types.
/// </summary>
internal interface IPlantLogic : ICharacterLogic<PlantDefinition, Plant>;
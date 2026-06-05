using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Reloaded.Versus;

namespace ReplantedOnline.Interfaces.Versus;

/// <summary>
/// Defines the contract for arena behavior in versus gamemode.
/// </summary>
internal interface IArena
{
    /// <summary>
    /// Gets the type identifier for this arena.
    /// </summary>
    ArenaTypes Type { get; }

    /// <summary>
    /// Gets the default spawn type for zombies in this arena.
    /// </summary>
    SpawnType DefaultZombieSpawnType { get; }

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

    /// <summary>
    /// Retrieves the current active arena instance.
    /// </summary>
    /// <returns>The currently active IArena implementation, or null if no matching arena is found</returns>
    internal static IArena GetCurrentArena()
    {
        foreach (var arena in RegisterArena.Instances)
        {
            if (arena.Type == VersusState.Arena)
            {
                return arena;
            }
        }

        return default!;
    }
}
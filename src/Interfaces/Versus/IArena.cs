using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Modules.Versus;

namespace ReplantedOnline.Interfaces.Versus;

/// <summary>
/// Defines the contract for arena-specific behavior in versus gamemode.
/// Implementations provide custom logic for different arena types.
/// </summary>
internal interface IArena
{
    /// <summary>
    /// Gets the type identifier for this arena.
    /// </summary>
    ArenaTypes Type { get; }

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

        return null;
    }
}
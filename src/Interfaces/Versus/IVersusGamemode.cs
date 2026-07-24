using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Reloaded.Versus;

namespace ReplantedOnline.Interfaces.Versus;

/// <summary>
/// Defines the contract for versus game mode implementations.
/// Provides lifecycle methods that are called during different phases of versus mode.
/// </summary>
internal interface IVersusGamemode
{
    /// <summary>
    /// Called when the versus game mode starts.
    /// </summary>
    /// <param name="versusMode">The instance of VersusMode.</param>
    void OnGameModeStart(VersusMode versusMode);

    /// <summary>
    /// Called when the versus gameplay starts.
    /// </summary>
    /// <param name="versusMode">The instance of VersusMode.</param>
    void OnGameplayStart(VersusMode versusMode);

    /// <summary>
    /// Called every frame during the versus game mode's active state.
    /// </summary>
    /// <param name="versusMode">The instance of VersusMode.</param>
    void UpdateGameplay(VersusMode versusMode);

    /// <summary>
    /// Called when the versus game mode ends.
    /// </summary>
    /// <param name="versusMode">The instance of VersusMode.</param>
    /// <param name="winningTeam">The team that won the match.</param>
    void OnGameplayEnd(VersusMode versusMode, PlayerTeam winningTeam);

    private static IVersusGamemode? currentGamemodeCached;
    /// <summary>
    /// Captures the current gamemode instance from the registered gamemode lookup.
    /// </summary>
    internal static void CatchCurrentGamemode()
    {
        currentGamemodeCached = RegisterVersusGamemode.GetInstanceFromLookup(VersusState.GamemodeSynced)!;
    }

    /// <summary>
    /// Retrieves the current versus gamemode.
    /// </summary>
    /// <returns>The currently active IVersusGamemode implementation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no gamemode instance is cached.</exception>
    internal static IVersusGamemode GetCurrentGamemode()
    {
        return currentGamemodeCached!;
    }
}
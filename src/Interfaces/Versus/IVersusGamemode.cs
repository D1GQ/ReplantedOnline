using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Modules.Versus.Gamemodes;

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

    /// <summary>
    /// Retrieves the current versus gamemode.
    /// </summary>
    /// <returns>
    /// The appropriate IVersusGamemode implementation for the current selection:
    /// Returns null if the SelectionSet doesn't match any known gamemode.
    /// </returns>
    internal static IVersusGamemode GetCurrentGamemode()
    {
        return VersusState.SelectionSet switch
        {
            SelectionSet.QuickPlay => RegisterVersusGameMode.GetInstance<QuickplayGamemode>(),
            SelectionSet.Random => RegisterVersusGameMode.GetInstance<RandomGamemode>(),
            SelectionSet.CustomAll => RegisterVersusGameMode.GetInstance<CustomGamemode>(),
            _ => null,
        };
    }
}
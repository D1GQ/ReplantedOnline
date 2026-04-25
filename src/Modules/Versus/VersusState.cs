using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Structs;

namespace ReplantedOnline.Modules.Versus;

/// <summary>
/// Provides centralized access to versus mode multiplayer state information.
/// </summary>
internal static class VersusState
{
    /// <summary>
    /// Gets the current phase of the versus match.
    /// </summary>
    internal static VersusPhase VersusPhase => Instances.GameplayActivity?.VersusMode?.Phase ?? VersusPhase.PickSides;

    /// <summary>
    /// Gets the current selection set being used for the versus match.
    /// </summary>
    internal static SelectionSet SelectionSet => Instances.GameplayActivity?.VersusMode?.SelectionSet ?? SelectionSet.QuickPlay;

    /// <summary>
    /// Determines if the local player is currently on the zombie team.
    /// </summary>
    internal static bool AmZombieSide => ReplantedClientData.LocalClient?.Team == PlayerTeam.Zombies;

    /// <summary>
    /// Determines if the local player is currently on the plant team.
    /// </summary>
    internal static bool AmPlantSide => ReplantedClientData.LocalClient?.Team == PlayerTeam.Plants;

    /// <summary>
    /// Determines if the local player is currently spectating..
    /// </summary>
    internal static bool AmSpectator => ReplantedClientData.LocalClient?.Team == PlayerTeam.Spectators;

    /// <summary>
    /// Gets the Steam ID of the player currently assigned to the plant team.
    /// </summary>
    internal static ID PlantClientId => ReplantedClientData.GetPlantClient()?.ClientId ?? 0;

    /// <summary>
    /// Gets the Steam ID of the player currently assigned to the zombie team.
    /// </summary>
    internal static ID ZombieClientId => ReplantedClientData.GetZombieClient()?.ClientId ?? 0;

    /// <summary>
    /// Gets the current arena type.
    /// </summary>
    internal static ArenaTypes Arena => ReplantedLobby.LobbyData?.Synced_Arena ?? ArenaTypes.Day;

    /// <summary>
    /// Gets when Versus Mode is in its gameplay state.
    /// </summary>
    internal static bool IsInGameplay => !IsInCountDown && VersusPhase is VersusPhase.Gameplay or VersusPhase.SuddenDeath;

    /// <summary>
    /// Gets when Versus Mode is in read, set, plant.
    /// </summary>
    internal static bool IsInCountDown => Instances.GameplayActivity.VersusMode?.m_versusTime <= 3.2f;
}
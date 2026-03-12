using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Structs;

namespace ReplantedOnline.Modules.Versus;

/// <summary>
/// Provides centralized access to versus (PvP) multiplayer state information.
/// This static class aggregates gameplay state, team assignments, and player identifiers for easy access throughout the mod.
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
    internal static bool AmZombieSide => NetClient.LocalClient?.Team == PlayerTeam.Zombies;

    /// <summary>
    /// Determines if the local player is currently on the plant team.
    /// </summary>
    internal static bool AmPlantSide => NetClient.LocalClient?.Team == PlayerTeam.Plants;

    /// <summary>
    /// Determines if the local player is currently spectating..
    /// </summary>
    internal static bool AmSpectator => NetClient.LocalClient?.Team == PlayerTeam.Spectators;

    /// <summary>
    /// Gets the Steam ID of the player currently assigned to the plant team.
    /// </summary>
    internal static ID PlantClientId => NetClient.GetPlantClient()?.ClientId ?? 0;

    /// <summary>
    /// Gets the Steam ID of the player currently assigned to the zombie team.
    /// </summary>
    internal static ID ZombieClientId => NetClient.GetZombieClient()?.ClientId ?? 0;
}
using Il2CppReloaded.Gameplay;
using Il2CppSteamworks;
using ReplantedOnline.Network;

namespace ReplantedOnline.Modules;

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
    internal static bool ZombieSide => SteamNetClient.LocalClient?.AmZombieSide() == true;

    /// <summary>
    /// Determines if the local player is currently on the plant team.
    /// </summary>
    internal static bool PlantSide => SteamNetClient.LocalClient?.AmPlantSide() == true;

    /// <summary>
    /// Gets the Steam ID of the player currently assigned to the plant team.
    /// </summary>
    internal static SteamId PlantSteamId => SteamNetClient.GetPlantClient()?.SteamId ?? 0;

    /// <summary>
    /// Gets the Steam ID of the player currently assigned to the zombie team.
    /// </summary>
    internal static SteamId ZombieSteamId => SteamNetClient.GetZombieClient()?.SteamId ?? 0;
}
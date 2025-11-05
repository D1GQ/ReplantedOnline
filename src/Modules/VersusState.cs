using Il2CppReloaded.Gameplay;
using Il2CppSteamworks;
using ReplantedOnline.Items.Enums;
using ReplantedOnline.Network;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Modules;

internal static class VersusState
{
    internal static GameState GameState => NetLobby.LobbyData?.LastGameState ?? GameState.Lobby;
    internal static VersusPhase VersusPhase => Instances.GameplayActivity?.VersusMode?.Phase ?? VersusPhase.PickSides;
    internal static SelectionSet SelectionSet => Instances.GameplayActivity?.VersusMode?.SelectionSet ?? SelectionSet.QuickPlay;
    internal static bool ZombieSide => SteamNetClient.LocalClient?.AmZombieSide() == true;
    internal static bool PlantSide => SteamNetClient.LocalClient?.AmZombieSide() == false;
    internal static SteamId PlantSteamId => SteamNetClient.GetPlantClient()?.SteamId ?? 0;
    internal static SteamId ZombieSteamId => SteamNetClient.GetZombieClient()?.SteamId ?? 0;
}

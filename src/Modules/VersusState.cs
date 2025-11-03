using Il2CppReloaded.Gameplay;
using ReplantedOnline.Items.Enums;
using ReplantedOnline.Network;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Modules;

internal static class VersusState
{
    internal static GameState GameState => NetLobby.LobbyData?.LastGameState ?? GameState.Lobby;
    internal static VersusPhase VersusPhase => Instances.GameplayActivity.VersusMode.Phase;
    internal static bool ZombieSide => SteamNetClient.LocalClient?.AmZombieSide() == true;
    internal static bool PlantSide => SteamNetClient.LocalClient?.AmZombieSide() == false;
}

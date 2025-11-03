using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppSource.DataModels;
using ReplantedOnline.Items.Enums;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Patches.Versus;

[HarmonyPatch]
internal static class VersusModePatch
{
    // TODO: Classes to look at to sync actions, Ahhhhhh I HATE Il2Cpp :(
    // VersusMode : ReloadedMode
    // VersusDataModel : DisposableObjectModel
    // VersusPlayerModel : DisposableObjectModel
    // VersusChooserSwapBinder : Binder
    // VersusWinDataResetActivity : InjectableActivity
    // Board : Widget
    // GameplayActivity : InjectableActivity
    // SeedChooserScreen : Widget

    [HarmonyPatch(typeof(VersusMode), nameof(VersusMode.InitializeGameplay))]
    [HarmonyPostfix]
    internal static void InitializeGameplay_Postfix()
    {
        VersusManager.OnStart();
    }

    // Stop game from placing initial sunflower in vs
    [HarmonyPatch(typeof(Board), nameof(Board.AddPlant))]
    [HarmonyPrefix]
    internal static bool AddPlant_Prefix()
    {
        if (NetLobby.AmInLobby() && Instances.GameplayActivity.VersusMode.m_versusTime < 1f)
        {
            return false;
        }

        return true;
    }

    // Stop game from placing initial gravestones in vs
    [HarmonyPatch(typeof(Challenge), nameof(Challenge.IZombiePlaceZombie))]
    [HarmonyPrefix]
    internal static bool IZombiePlaceZombie_Prefix(ZombieType theZombieType)
    {
        if (NetLobby.AmInLobby() && Instances.GameplayActivity.VersusMode.m_versusTime < 1f)
        {
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(VersusPlayerModel), nameof(VersusPlayerModel.Confirm))]
    [HarmonyPostfix]
    internal static void Confirm_Postfix(VersusPlayerModel __instance)
    {
        if (!NetLobby.AmLobbyHost()) return;

        if (Instances.GameplayActivity.VersusMode.PlantPlayerIndex == 0)
        {
            NetLobby.LobbyData.UpdateGameState(GameState.HostChoosePlants);
        }
        else
        {
            NetLobby.LobbyData.UpdateGameState(GameState.HostChooseZombie);
        }
    }

    [HarmonyPatch(typeof(VersusPlayerModel), nameof(VersusPlayerModel.Cancel))]
    [HarmonyPostfix]
    internal static void Cancel_Postfix(VersusPlayerModel __instance)
    {
        if (!NetLobby.AmLobbyHost()) return;

        NetLobby.LobbyData.UpdateGameState(GameState.Lobby);
    }

    [HarmonyPatch(typeof(VersusPlayerModel), nameof(VersusPlayerModel.Move))]
    [HarmonyPostfix]
    internal static void Move_Postfix(VersusPlayerModel __instance, float x)
    {
    }
}

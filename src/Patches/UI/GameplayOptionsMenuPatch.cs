using HarmonyLib;
using Il2CppReloaded.UI;
using ReplantedOnline.Helper;
using ReplantedOnline.Network.Online;
using UnityEngine.UI;

namespace ReplantedOnline.Patches.UI;

[HarmonyPatch]
internal class GameplayOptionsMenuPatch
{
    [HarmonyPatch(typeof(GameplayOptionsMenu), nameof(GameplayOptionsMenu.OnEnable))]
    [HarmonyPostfix]
    internal static void ActiveStarted_Postfix(GameplayOptionsMenu __instance)
    {
        // Only modify the menu if we're in an online lobby
        if (NetLobby.IsInLobby())
        {
            // Remove restart button during online matches (can't restart mid-game)
            var restartLevel = __instance.transform.Find("P_OptionsPanel_Canvas/Layout/Center/Panel/Bottom/Buttons/Hlayout/P_BasicButton_RestartLevel")?.gameObject;
            if (restartLevel != null)
            {
                UnityEngine.Object.Destroy(restartLevel);
            }

            // Replace main menu button with lobby leave functionality
            var mainMneuButton = __instance.transform.Find("P_OptionsPanel_Canvas/Layout/Center/Panel/Bottom/Buttons/Hlayout/P_BasicButton_MainMenu")?.GetComponentInChildren<Button>(true);
            mainMneuButton.onClick = new();
            mainMneuButton.onClick.AddListener(NetLobby.LeaveLobby);
        }
    }
}
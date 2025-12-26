using BloomEngine.Utilities;
using HarmonyLib;
using Il2CppReloaded.UI;
using Il2CppTMPro;
using ReplantedOnline.Helper;
using ReplantedOnline.Network.Online;
using UnityEngine.UI;

namespace ReplantedOnline.Patches.Client.UI;

[HarmonyPatch]
internal static class PauseMenuPatch
{
    [HarmonyPatch(typeof(GameplayOptionsMenu), nameof(GameplayOptionsMenu.OnEnable))]
    [HarmonyPostfix]
    private static void GameplayOptionsMenu_OnEnable_Postfix(GameplayOptionsMenu __instance)
    {
        // Only modify the menu if we're in an online lobby
        if (NetLobby.AmInLobby())
        {
            var restartLevelButton = __instance.transform.Find("P_OptionsPanel_Canvas/Layout/Center/Panel/Bottom/Buttons/Hlayout/P_BasicButton_RestartLevel")?.GetComponentInChildren<Button>(true);
            if (restartLevelButton != null && NetLobby.AmLobbyHost())
            {
                restartLevelButton.onClick = new();
                restartLevelButton.onClick.AddListener(() =>
                {
                    NetLobby.LobbyData?.Networked?.ResetLobby();
                });
                restartLevelButton.gameObject.DestroyAllTextLocalizers();
                restartLevelButton.GetComponentInChildren<TextMeshProUGUI>(true)?.SetText("Restart Lobby");
            }
            else
            {
                UnityEngine.Object.Destroy(restartLevelButton.gameObject);
            }

            // Replace main menu button with lobby leave functionality
            var mainMneuButton = __instance.transform.Find("P_OptionsPanel_Canvas/Layout/Center/Panel/Bottom/Buttons/Hlayout/P_BasicButton_MainMenu")?.GetComponentInChildren<Button>(true);
            if (mainMneuButton != null)
            {
                mainMneuButton.onClick = new();
                mainMneuButton.onClick.AddListener(() => NetLobby.LeaveLobby());
            }

            // Remove almanac button
            var almanacButton = __instance.transform.Find("P_OptionsPanel_Canvas/Layout/Center/P_ControllerPrompt_Legend");
            if (almanacButton != null)
            {
                UnityEngine.Object.Destroy(almanacButton.gameObject);
            }
        }
    }
}
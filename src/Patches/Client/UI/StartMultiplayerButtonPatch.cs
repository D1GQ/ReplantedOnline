using HarmonyLib;
using Il2CppSource.Binders;
using Il2CppTMPro;
using ReplantedOnline.Helper;
using ReplantedOnline.Network.Online;
using UnityEngine.UI;

namespace ReplantedOnline.Patches.Client.UI;

[HarmonyPatch]
internal static class StartMultiplayerButtonPatch
{
    [HarmonyPatch(typeof(StartMultiplayerButton), nameof(StartMultiplayerButton.Awake))]
    [HarmonyPrefix]
    private static void StartMultiplayerButton_Awake_Postfix(StartMultiplayerButton __instance)
    {
        // Remove existing text localization components
        __instance.gameObject.DestroyAllTextLocalizers();

        // Get references to button and text components
        var button = __instance.GetComponentInChildren<Button>(true);
        var texts = __instance.GetComponentsInChildren<TextMeshProUGUI>(true);

        // Check if this is the "VS Button" (Join button) or Host button
        if (__instance.gameObject.name == "CoopVS_VS_Button")
        {
            // Update all text elements to say "Join"
            foreach (var textComp in texts)
            {
                textComp.SetText("Join");
            }

            // Set up button click handler
            if (button != null)
            {
                button.onClick = new();
                button.onClick.AddListener((Action)(() =>
                {
                    __instance._onButtonClicked();
                }));
            }
        }
        else
        {
            // Update all text elements to say "Host"
            foreach (var textComp in texts)
            {
                textComp.SetText("Host");
            }

            // Set up button click handler
            if (button != null)
            {
                button.onClick = new();
                button.onClick.AddListener((Action)(() =>
                {
                    __instance._onButtonClicked();
                }));
            }
        }
    }

    [HarmonyPatch(typeof(StartMultiplayerButton), nameof(StartMultiplayerButton._onButtonClicked))]
    [HarmonyPrefix]
    private static bool StartMultiplayerButton_OnButtonClicked_Prefix(StartMultiplayerButton __instance)
    {
        // Determine if this is the Host button or Join button
        if (__instance.gameObject.name != "CoopVS_VS_Button")
        {
            // Host button clicked - create a new lobby
            NetLobby.CreateLobby();
        }
        else
        {
            // Join button clicked - show the lobby code input panel
            JoinLobbyCodePanelPatch.ShowLobbyCodePanel();
        }

        // Return false to prevent the original method from running
        // This replaces the default multiplayer behavior with custom online functionality
        return false;
    }
}
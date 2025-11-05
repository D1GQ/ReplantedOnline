using HarmonyLib;
using Il2CppReloaded.Input;
using Il2CppTekly.DataModels.Binders;
using Il2CppTekly.PanelViews;
using Il2CppTMPro;
using MelonLoader;
using ReplantedOnline.Helper;
using ReplantedOnline.Network.Online;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ReplantedOnline.Patches.UI;

[HarmonyPatch]
internal static class JoinLobbyCodePanelPatch
{
    // Store references to the lobby code panel and input field
    private static PanelView _lobbyCodePanel;
    private static ReloadedInputField _reloadedInputField;

    [HarmonyPatch(typeof(PanelViewContainer), nameof(PanelViewContainer.Awake))]
    [HarmonyPostfix]
    internal static void Awake_Postfix(PanelViewContainer __instance)
    {
        // Check if this is the frontend panels container
        if (__instance.name == "FrontendPanels")
        {
            // Find the existing users rename panel to use as a template
            var usersRenamePanel = __instance.m_panels.FirstOrDefault(p => p.Id == "usersRename");
            if (usersRenamePanel != null)
            {
                // Create a copy of the rename panel for our lobby code input
                _lobbyCodePanel = UnityEngine.Object.Instantiate(usersRenamePanel, __instance.transform);
                _lobbyCodePanel.m_id = "joinLobbyCode";
                _lobbyCodePanel.name = "P_Join_LobbyCode";
                // Customize the panel for lobby code input
                SetUplobbyCodePanel(_lobbyCodePanel);
            }
        }
    }

    // Method to show the lobby code input panel
    internal static void ShowLobbyCodePanel()
    {
        _lobbyCodePanel?.gameObject.SetActive(true);
        _reloadedInputField?.SetText(string.Empty, false);
    }

    // Configure the lobby code panel with custom text and behavior
    private static void SetUplobbyCodePanel(PanelView lobbyCodePanel)
    {
        // Remove existing text localization components
        lobbyCodePanel.DestroyAllTextLocalizers();

        // Get reference to the input field and set up validation
        _reloadedInputField = GetComp<ReloadedInputField>("Canvas/Layout/Center/Rename/NameInputField");
        _reloadedInputField.characterLimit = 6;
        _reloadedInputField.onValueChanged.AddListener((Action<string>)((newValue) =>
        {
            // Filter input to only allow valid lobby code characters
            string cleanValue = new([.. newValue.Where(c => NetLobbyData.CODE_CHARS.Contains(char.ToUpper(c))).Select(char.ToUpper)]);
            _reloadedInputField.m_Text = string.Empty;
            // Use coroutine to update text after current frame
            MelonCoroutines.Start(CoSetTextDelay(newValue, cleanValue));
        }));

        // Update all text elements in the panel
        SetText("Canvas/Layout/Center/Rename/HeaderText", "Join Lobby");
        SetText("Canvas/Layout/Center/Rename/SubheadingText", "Please enter lobby code:");
        SetText("Canvas/Layout/Center/Rename/NameInputField/Text Area/Placeholder", "Enter code...");

        // Set up OK button to search for lobby with entered code
        SetButton("Canvas/Layout/Center/Rename/Buttons/P_BacicButton_OK", () =>
        {
            if (_reloadedInputField.m_Text.Length == 6)
            {
                lobbyCodePanel.gameObject.SetActive(false);
                NetLobby.SearchLobbyByGameCode(_reloadedInputField.m_Text);
            }
        });

        // Set up Cancel button to simply close the panel
        SetButton("Canvas/Layout/Center/Rename/Buttons/P_BacicButton_Cancel", () =>
        {
            lobbyCodePanel.gameObject.SetActive(false);
        });
    }

    // Coroutine to delay text setting until after input field updates
    private static IEnumerator CoSetTextDelay(string newValue, string cleanValue)
    {
        // Wait until the input field text matches what we're trying to set
        while (_reloadedInputField.m_Text != newValue)
        {
            yield return null;
        }
        // Apply the filtered text
        _reloadedInputField?.SetText(cleanValue, false);
        _reloadedInputField?.ForceLabelUpdate();
    }

    // Helper method to set text on TextMeshPro components
    private static void SetText(string path, string text)
    {
        GetComp<TextMeshProUGUI>(path)?.SetText(text);
    }

    // Helper method to set up button click handlers
    private static void SetButton(string path, Action callback)
    {
        var button = GetComp<Button>(path);
        var buttonBinder = GetComp<UnityButtonBinder>(path);
        // Remove existing button binder to override behavior
        if (buttonBinder != null)
        {
            UnityEngine.Object.Destroy(buttonBinder);
        }
        if (button != null)
        {
            button.onClick = new();
            button.onClick.AddListener(callback);
        }
    }

    // Helper method to find components in the panel hierarchy
    private static T GetComp<T>(string path) where T : MonoBehaviour
    {
        return _lobbyCodePanel?.transform?.Find(path)?.GetComponentInChildren<T>(true);
    }
}
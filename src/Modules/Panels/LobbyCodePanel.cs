using BloomEngine.Extensions;
using Il2CppReloaded.Input;
using Il2CppTekly.DataModels.Binders;
using Il2CppTekly.PanelViews;
using Il2CppTMPro;
using ReplantedOnline.Managers;
using ReplantedOnline.Utilities;
using UnityEngine.UI;

namespace ReplantedOnline.Modules.Panels;

/// <summary>
/// Manages the lobby code entry panel for joining games via lobby codes.
/// </summary>
internal static class LobbyCodePanel
{
    private static PanelView _lobbyCodePanel;
    private static ReloadedInputField _reloadedInputField;
    private static string lastText = string.Empty;
    private static bool _hasInit;

    /// <summary>
    /// Initializes the lobby code panel by cloning a template panel and setting up all UI components.
    /// </summary>
    /// <param name="usersRenamePanel">The template panel view to clone for creating the lobby code panel.</param>
    internal static void Init(PanelView usersRenamePanel)
    {
        if (_hasInit) return;
        _hasInit = true;

        _lobbyCodePanel = UnityEngine.Object.Instantiate(usersRenamePanel, Instances.GlobalPanels.transform);
        _lobbyCodePanel.m_id = "joinLobbyCode";
        _lobbyCodePanel.name = "P_Join_LobbyCode";

        // Remove existing text localization components
        _lobbyCodePanel.m_id = "I_LobbyCodePanel";
        _lobbyCodePanel.gameObject.DestroyAllTextLocalizers();

        // Get reference to the input field and set up validation
        _reloadedInputField = _lobbyCodePanel?.transform?.Find("Canvas/Layout/Center/Rename/NameInputField")?.GetComponentInChildren<ReloadedInputField>(true);
        if (_reloadedInputField != null)
        {
            _reloadedInputField.characterLimit = MatchmakingManager.CODE_LENGTH;
            _reloadedInputField.onValueChanged = null;
        }

        // Update all text elements in the panel
        var headerText = _lobbyCodePanel?.transform?.Find("Canvas/Layout/Center/Rename/HeaderText")?.GetComponentInChildren<TextMeshProUGUI>(true);
        headerText?.SetText("Join Lobby");

        var subheadingText = _lobbyCodePanel?.transform?.Find("Canvas/Layout/Center/Rename/SubheadingText")?.GetComponentInChildren<TextMeshProUGUI>(true);
        subheadingText?.SetText("Please enter lobby code:");

        var placeholderText = _lobbyCodePanel?.transform?.Find("Canvas/Layout/Center/Rename/NameInputField/Text Area/Placeholder")?.GetComponentInChildren<TextMeshProUGUI>(true);
        placeholderText?.SetText("Enter code...");

        // Set up OK button to search for lobby with entered code
        var okButton = _lobbyCodePanel?.transform?.Find("Canvas/Layout/Center/Rename/Buttons/P_BacicButton_OK")?.GetComponentInChildren<Button>(true);
        if (okButton != null)
        {
            var okButtonBinder = _lobbyCodePanel?.transform?.Find("Canvas/Layout/Center/Rename/Buttons/P_BacicButton_OK")?.GetComponentInChildren<UnityButtonBinder>(true);
            if (okButtonBinder != null)
            {
                UnityEngine.Object.Destroy(okButtonBinder);
            }

            okButton.onClick = new();
            okButton.onClick.AddListener(() =>
            {
                if (_reloadedInputField != null && _reloadedInputField.m_Text.Length == MatchmakingManager.CODE_LENGTH)
                {
                    _lobbyCodePanel.gameObject.SetActive(false);
                    MatchmakingManager.SearchSteamLobbyByGameCode(_reloadedInputField.m_Text.ToUpper());
                }
                else
                {
                    ReplantedOnlinePopup.Show("Error", $"Lobby code must contain {MatchmakingManager.CODE_LENGTH} characters!");
                }
            });
        }

        // Set up Cancel button to simply close the panel
        var cancelButton = _lobbyCodePanel?.transform?.Find("Canvas/Layout/Center/Rename/Buttons/P_BacicButton_Cancel")?.GetComponentInChildren<Button>(true);
        if (cancelButton != null)
        {
            var cancelButtonBinder = _lobbyCodePanel?.transform?.Find("Canvas/Layout/Center/Rename/Buttons/P_BacicButton_Cancel")?.GetComponentInChildren<UnityButtonBinder>(true);
            if (cancelButtonBinder != null)
            {
                UnityEngine.Object.Destroy(cancelButtonBinder);
            }

            cancelButton.onClick = new();
            cancelButton.onClick.AddListener((System.Action)(() =>
            {
                _lobbyCodePanel.gameObject.SetActive(false);
            }));
        }
    }

    /// <summary>
    /// Displays the lobby code panel and resets the input field.
    /// </summary>
    internal static void Show()
    {
        _lobbyCodePanel?.gameObject.SetActive(true);
        _reloadedInputField?.SetText(string.Empty, false);
    }

    /// <summary>
    /// Validates and sanitizes the input field text to ensure it only contains allowed characters.
    /// </summary>
    internal static void ValidateText()
    {
        if (_reloadedInputField != null)
        {
            if (_reloadedInputField.text != lastText)
            {
                string currentText = _reloadedInputField.text;
                string cleanValue = new([.. currentText.Where(c => MatchmakingManager.CODE_CHARS.Contains(char.ToUpper(c))).Select(char.ToUpper)]);

                _reloadedInputField?.SetText(cleanValue, false);
                _reloadedInputField?.ForceLabelUpdate();
                lastText = _reloadedInputField.text;
            }
        }
    }
}
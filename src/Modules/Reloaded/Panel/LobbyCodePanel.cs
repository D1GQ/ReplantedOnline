using BloomEngine.Extensions;
using Il2CppReloaded.Input;
using Il2CppTekly.DataModels.Binders;
using Il2CppTekly.PanelViews;
using Il2CppTMPro;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Utilities.Unity;
using UnityEngine.UI;

namespace ReplantedOnline.Modules.Reloaded.Panel;

/// <summary>
/// Manages the lobby code entry panel for joining games via lobby codes.
/// </summary>
internal static class LobbyCodePanel
{
    private static PanelView? Panel;
    private static ReloadedInputField? InputField;
    private static string LastText = string.Empty;
    private static bool HasInit;

    /// <summary>
    /// Initializes the lobby code panel by cloning a template panel and setting up all UI components.
    /// </summary>
    /// <param name="usersRenamePanel">The template panel view to clone for creating the lobby code panel.</param>
    internal static void Init(PanelView usersRenamePanel)
    {
        if (HasInit) return;
        HasInit = true;

        Panel = UnityEngine.Object.Instantiate(usersRenamePanel, Instances.GlobalPanels.transform);
        Panel.m_id = "joinLobbyCode";
        Panel.name = "P_Join_LobbyCode";

        // Remove existing text localization components
        Panel.m_id = "I_LobbyCodePanel";
        Panel.gameObject.DestroyAllTextLocalizers();

        // Get reference to the input field and set up validation
        InputField = Panel?.transform?.Find("Canvas/Layout/Center/Rename/NameInputField")?.GetComponentInChildren<ReloadedInputField>(true)!;
        if (InputField != null)
        {
            InputField.characterLimit = ReloadedMatchmaking.CODE_LENGTH;
            InputField.onValueChanged = null;
        }

        // Update all text elements in the panel
        var headerText = Panel?.transform?.Find("Canvas/Layout/Center/Rename/HeaderText")?.GetComponentInChildren<TextMeshProUGUI>(true);
        headerText?.SetText("Join Lobby");

        var subheadingText = Panel?.transform?.Find("Canvas/Layout/Center/Rename/SubheadingText")?.GetComponentInChildren<TextMeshProUGUI>(true);
        subheadingText?.SetText("Please enter lobby code:");

        var placeholderText = Panel?.transform?.Find("Canvas/Layout/Center/Rename/NameInputField/Text Area/Placeholder")?.GetComponentInChildren<TextMeshProUGUI>(true);
        placeholderText?.SetText("Enter code...");

        // Set up OK button to search for lobby with entered code
        var okButton = Panel?.transform?.Find("Canvas/Layout/Center/Rename/Buttons/P_BacicButton_OK")?.GetComponentInChildren<Button>(true);
        if (okButton != null)
        {
            var okButtonBinder = Panel?.transform?.Find("Canvas/Layout/Center/Rename/Buttons/P_BacicButton_OK")?.GetComponentInChildren<UnityButtonBinder>(true);
            if (okButtonBinder != null)
            {
                UnityEngine.Object.Destroy(okButtonBinder);
            }

            okButton.onClick = new();
            okButton.onClick.AddListener(() =>
            {
                if (InputField != null && InputField.m_Text.Length == ReloadedMatchmaking.CODE_LENGTH)
                {
                    Panel?.gameObject.SetActive(false);
                    string gameCode = InputField.m_Text.ToUpper();

                    ReloadedMatchmaking.JoinSteamLobbyByGameCode(gameCode);
                }
                else
                {
                    CustomPopupPanel.Show("Error", $"Lobby code must contain {ReloadedMatchmaking.CODE_LENGTH} characters!");
                }
            });
        }

        // Set up Cancel button to simply close the panel
        var cancelButton = Panel?.transform?.Find("Canvas/Layout/Center/Rename/Buttons/P_BacicButton_Cancel")?.GetComponentInChildren<Button>(true);
        if (cancelButton != null)
        {
            var cancelButtonBinder = Panel?.transform?.Find("Canvas/Layout/Center/Rename/Buttons/P_BacicButton_Cancel")?.GetComponentInChildren<UnityButtonBinder>(true);
            if (cancelButtonBinder != null)
            {
                UnityEngine.Object.Destroy(cancelButtonBinder);
            }

            cancelButton.onClick = new();
            cancelButton.onClick.AddListener((Action)(() =>
            {
                Panel?.gameObject.SetActive(false);
            }));
        }
    }

    /// <summary>
    /// Displays the lobby code panel and resets the input field.
    /// </summary>
    internal static void Show()
    {
        Panel?.gameObject.SetActive(true);
        InputField?.SetText(string.Empty, false);
    }

    /// <summary>
    /// Validates and sanitizes the input field text to ensure it only contains allowed characters.
    /// </summary>
    internal static void ValidateText()
    {
        if (InputField != null)
        {
            if (InputField.text != LastText)
            {
                string currentText = InputField.text;
                string cleanValue = new([.. currentText.Where(c => ReloadedMatchmaking.CODE_CHARS.Contains(char.ToUpper(c))).Select(char.ToUpper)]);

                InputField?.SetText(cleanValue, false);
                InputField?.ForceLabelUpdate();
                LastText = InputField?.text ?? string.Empty;
            }
        }
    }
}
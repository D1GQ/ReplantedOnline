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
/// Manages the direct IP entry panel for joining games via host:port.
/// </summary>
internal static class DirectIpPanel
{
    private static event Action? OnShow;
    private static PanelView? Panel;
    private static ReloadedInputField? InputField;
    private static string LastText = string.Empty;
    private static bool HasInit;

    /// <summary>
    /// Initializes the panel by cloning a template panel and setting up all UI components.
    /// </summary>
    /// <param name="usersRenamePanel">The template panel view to clone for creating the panel.</param>
    internal static void Init(PanelView usersRenamePanel)
    {
        if (HasInit)
            return;

        HasInit = true;

        Panel = UnityEngine.Object.Instantiate(usersRenamePanel, Instances.GlobalPanels.transform);
        Panel.m_id = "directIp";
        Panel.name = "P_Join_DirectIp";

        Panel.m_id = "I_DirectIpPanel";

        // Get reference to the input field and set up validation
        InputField = Panel?.transform?.Find("Canvas/Layout/Center/Rename/NameInputField")?.GetComponentInChildren<ReloadedInputField>(true)!;
        if (InputField != null)
        {
            InputField.characterLimit = 0; // unlimited — host:port is variable length
            InputField.onValueChanged = null;
        }

        // Update all text elements in the panel
        var headerText = Panel?.transform?.Find("Canvas/Layout/Center/Rename/HeaderText")?.GetComponentInChildren<TextMeshProUGUI>(true);
        headerText?.SetTextLocalizer("$MESSAGE_JOIN_IP");

        var subheadingText = Panel?.transform?.Find("Canvas/Layout/Center/Rename/SubheadingText")?.GetComponentInChildren<TextMeshProUGUI>(true);
        subheadingText?.SetTextLocalizer("$MESSAGE_PLEASE_ENTER_IP");

        var placeholderText = Panel?.transform?.Find("Canvas/Layout/Center/Rename/NameInputField/Text Area/Placeholder")?.GetComponentInChildren<TextMeshProUGUI>(true);
        placeholderText?.SetTextLocalizer("$MESSAGE_ENTER_IP");

        // Set up OK button to join by host:port
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
                if (InputField == null)
                    return;

                string raw = InputField.m_Text.Trim();

                int port = 0;
                int colon = raw.LastIndexOf(':');
                bool valid = colon > 0 && colon < raw.Length - 1 && int.TryParse(raw.AsSpan(colon + 1), out port) && port >= 1 && port <= 65535;

                if (!valid)
                {
                    CustomPopupPanel.ShowLocalized("$POPUP_HEADER_ERROR", "$POPUP_LOBBY_IP_SYNTAX");
                    return;
                }

                string host = raw[..colon];
                Panel?.gameObject.SetActive(false);
                ReloadedMatchmaking.JoinIpLobbyByAddress(host, port);
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
    /// Displays the panel and resets the input field.
    /// </summary>
    internal static void Show()
    {
        OnShow?.Invoke();
        Panel?.gameObject.SetActive(true);
        InputField?.SetText(string.Empty, false);
    }

    /// <summary>
    /// Validates and sanitizes the input field text to ensure it only contains
    /// characters that can appear in a host:port string (letters, digits, '.', ':', '-').
    /// </summary>
    internal static void ValidateText()
    {
        if (InputField != null)
        {
            if (InputField.text != LastText)
            {
                string currentText = InputField.text;
                string cleanValue = new([.. currentText.Where(c => char.IsDigit(c) || c == '.' || c == ':' || c == '-')]);

                InputField.SetText(cleanValue, false);
                InputField.ForceLabelUpdate();
                LastText = InputField.text ?? string.Empty;
            }
        }
    }
}
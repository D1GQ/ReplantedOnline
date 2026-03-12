using BloomEngine.Extensions;
using Il2CppTekly.PanelViews;
using Il2CppTMPro;
using ReplantedOnline.Utilities;
using UnityEngine.UI;

namespace ReplantedOnline.Modules.Panels;

/// <summary>
/// Provides a custom popup dialog system for Replanted Online mod.
/// </summary>
internal static class ReplantedOnlinePopup
{
    private static PanelView _panel;
    private static TextMeshProUGUI _header;
    private static TextMeshProUGUI _subText;
    private static TextMeshProUGUI _label;
    private static bool _hasInit;

    /// <summary>
    /// Initializes the popup system by creating a custom popup panel from an existing template.
    /// </summary>
    /// <param name="globalPanels">The PanelViewContainer that contains the base panel template to clone from.</param>
    internal static void Init(PanelViewContainer globalPanels)
    {
        if (_hasInit) return;
        _hasInit = true;

        _panel = UnityEngine.Object.Instantiate(globalPanels.transform.Find("P_PopUpMessage02").GetComponentInChildren<PanelView>(true), globalPanels.transform);
        _panel.name = "P_PopUpReplantedOnline";
        _panel.m_id = "rPopup";
        _panel.gameObject.DestroyAllTextLocalizers();

        // Find and store reference to the header text component
        _header = _panel.transform.Find("Canvas/Layout/Center/Window/HeaderText").GetComponentInChildren<TextMeshProUGUI>(true);
        _header.gameObject.DestroyAllBinders();

        // Find and store reference to the subheading text component
        _subText = _panel.transform.Find("Canvas/Layout/Center/Window/SubheadingText").GetComponentInChildren<TextMeshProUGUI>(true);
        _subText.gameObject.DestroyAllBinders();

        // Find the OK button in the panel hierarchy
        var button = _panel.transform.Find("Canvas/Layout/Center/Window/Buttons/P_BacicButton_Ok").GetComponentInChildren<Button>(true);
        button.gameObject.DestroyAllBinders();
        button.gameObject.SetActive(true);

        // Find and store reference to the button's label text
        _label = button.transform.Find("Label").GetComponentInChildren<TextMeshProUGUI>(true);
        _label.SetText(string.Empty);

        // Set up new click handler for the button
        button.onClick = new();
        button.onClick.AddListener(() =>
        {
            SetButtonLabel(string.Empty);
            Hide();
        });
    }

    /// <summary>
    /// Sets the text label for the popup's confirmation button.
    /// </summary>
    /// <param name="label">The text to display on the button. If empty, defaults to "Ok" when shown.</param>
    internal static void SetButtonLabel(string label)
    {
        if (!_hasInit) return;

        _label?.SetText(label);
    }

    /// <summary>
    /// Displays the popup with the specified header and text content.
    /// </summary>
    /// <param name="header">The main header/title text for the popup.</param>
    /// <param name="text">The body/subtext content of the popup message.</param>
    internal static void Show(string header, string text)
    {
        if (!_hasInit) return;

        if (_label?.text == string.Empty)
        {
            _label?.SetText("Ok");
        }
        _panel?.gameObject?.SetActive(true);
        _header?.SetText(header);
        _subText?.SetText(text);
    }

    /// <summary>
    /// Hides the popup and clears all text content.
    /// </summary>
    internal static void Hide()
    {
        if (!_hasInit) return;

        _panel?.gameObject.SetActive(false);
        _header?.SetText(string.Empty);
        _subText?.SetText(string.Empty);
    }
}
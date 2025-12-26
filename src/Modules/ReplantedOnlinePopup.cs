using BloomEngine.Utilities;
using Il2CppTekly.PanelViews;
using Il2CppTMPro;
using ReplantedOnline.Helper;
using UnityEngine.UI;

namespace ReplantedOnline.Modules;

/// <summary>
/// Provides a custom popup dialog system for Replanted Online mod.
/// </summary>
internal static class ReplantedOnlinePopup
{
    private static PanelView Panel;
    private static TextMeshProUGUI Header;
    private static TextMeshProUGUI SubText;
    private static TextMeshProUGUI Label;
    private static bool _hasInit;

    /// <summary>
    /// Initializes the popup system by creating a custom popup panel from an existing template.
    /// </summary>
    /// <param name="globalPanels">The PanelViewContainer that contains the base panel template to clone from.</param>
    internal static void Init(PanelViewContainer globalPanels)
    {
        if (_hasInit) return;
        _hasInit = true;

        Panel = UnityEngine.Object.Instantiate(globalPanels.transform.Find("P_PopUpMessage02").GetComponentInChildren<PanelView>(true), globalPanels.transform);
        Panel.name = "P_PopUpReplantedOnline";
        Panel.m_id = "rPopup";
        Panel.gameObject.DestroyAllTextLocalizers();
        Header = Panel.transform.Find("Canvas/Layout/Center/Window/HeaderText").GetComponentInChildren<TextMeshProUGUI>(true);
        Header.gameObject.DestroyAllBinders();
        SubText = Panel.transform.Find("Canvas/Layout/Center/Window/SubheadingText").GetComponentInChildren<TextMeshProUGUI>(true);
        SubText.gameObject.DestroyAllBinders();
        var button = Panel.transform.Find("Canvas/Layout/Center/Window/Buttons/P_BacicButton_Ok").GetComponentInChildren<Button>(true);
        button.gameObject.DestroyAllBinders();
        button.gameObject.SetActive(true);
        Label = button.transform.Find("Label").GetComponentInChildren<TextMeshProUGUI>(true);
        Label.SetText(string.Empty);
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
        Label?.SetText(label);
    }

    /// <summary>
    /// Displays the popup with the specified header and text content.
    /// </summary>
    /// <param name="header">The main header/title text for the popup.</param>
    /// <param name="text">The body/subtext content of the popup message.</param>
    internal static void Show(string header, string text)
    {
        if (Label?.text == string.Empty)
        {
            Label?.SetText("Ok");
        }
        Panel?.gameObject?.SetActive(true);
        Header?.SetText(header);
        SubText?.SetText(text);
    }

    /// <summary>
    /// Hides the popup and clears all text content.
    /// </summary>
    internal static void Hide()
    {
        Panel?.gameObject.SetActive(false);
        Header?.SetText(string.Empty);
        SubText?.SetText(string.Empty);
    }
}
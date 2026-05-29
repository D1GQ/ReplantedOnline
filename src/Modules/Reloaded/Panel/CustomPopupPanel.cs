using BloomEngine.Extensions;
using Il2CppTekly.PanelViews;
using Il2CppTMPro;
using ReplantedOnline.Utilities.Unity;
using UnityEngine.UI;

namespace ReplantedOnline.Modules.Reloaded.Panel;

/// <summary>
/// Provides a custom popup dialog system for Replanted Online mod.
/// </summary>
internal static class CustomPopupPanel
{
    private static PanelView Panel;
    private static TextMeshProUGUI HeaderText;
    private static TextMeshProUGUI Text;
    private static TextMeshProUGUI LabelText;
    private static bool HasInit;

    /// <summary>
    /// Initializes the popup system by creating a custom popup panel from an existing template.
    /// </summary>
    /// <param name="globalPanels">The PanelViewContainer that contains the base panel template to clone from.</param>
    internal static void Init(PanelViewContainer globalPanels)
    {
        if (HasInit) return;
        HasInit = true;

        Panel = UnityEngine.Object.Instantiate(globalPanels.transform.Find("P_PopUpMessage02").GetComponentInChildren<PanelView>(true), globalPanels.transform);
        Panel.name = "P_PopUpReplantedOnline";
        Panel.m_id = "rPopup";
        Panel.gameObject.DestroyAllTextLocalizers();

        // Find and store reference to the header text component
        HeaderText = Panel.transform.Find("Canvas/Layout/Center/Window/HeaderText").GetComponentInChildren<TextMeshProUGUI>(true);
        HeaderText.gameObject.DestroyAllBinders();

        // Find and store reference to the subheading text component
        Text = Panel.transform.Find("Canvas/Layout/Center/Window/SubheadingText").GetComponentInChildren<TextMeshProUGUI>(true);
        Text.gameObject.DestroyAllBinders();

        // Find the OK button in the panel hierarchy
        var button = Panel.transform.Find("Canvas/Layout/Center/Window/Buttons/P_BacicButton_Ok").GetComponentInChildren<Button>(true);
        button.gameObject.DestroyAllBinders();
        button.gameObject.SetActive(true);

        // Find and store reference to the button's label text
        LabelText = button.transform.Find("Label").GetComponentInChildren<TextMeshProUGUI>(true);
        LabelText.SetText(string.Empty);

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
        if (!HasInit) return;

        LabelText?.SetText(label);
    }

    /// <summary>
    /// Displays the popup with the specified header and text content.
    /// </summary>
    /// <param name="header">The main header/title text for the popup.</param>
    /// <param name="text">The body/subtext content of the popup message.</param>
    internal static void Show(string header, string text)
    {
        if (!HasInit) return;

        if (LabelText?.text == string.Empty)
        {
            LabelText?.SetText("Ok");
        }
        Panel?.gameObject?.SetActive(true);
        HeaderText?.SetText(header);
        Text?.SetText(text);
    }

    /// <summary>
    /// Hides the popup and clears all text content.
    /// </summary>
    internal static void Hide()
    {
        if (!HasInit) return;

        Panel?.gameObject.SetActive(false);
        HeaderText?.SetText(string.Empty);
        Text?.SetText(string.Empty);
    }
}
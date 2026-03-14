using Il2CppReloaded.Data;
using Il2CppSource.UI;
using Il2CppTekly.PanelViews;
using Il2CppTMPro;
using ReplantedOnline.Enums;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace ReplantedOnline.Modules.Panel;

/// <summary>
/// Manages the creation and updating of the arena selection panel UI.
/// Handles displaying arena previews and synchronizing with lobby data.
/// </summary>
internal static class ArenaSelectorPanel
{
    private static GameObject _panel;
    private static Image _preview;

    /// <summary>
    /// Creates the arena selector panel by cloning an existing plant panel and configuring it for arena selection.
    /// </summary>
    /// <param name="VsSideChooser">The PanelView component from the VS side chooser UI, used as a parent container.</param>
    internal static void Create(PanelView VsSideChooser)
    {
        if (_panel != null) return;

        var VsPanels = VsSideChooser.transform.Find("Canvas/Layout/Center").gameObject;
        VsPanels.transform.Find("Panel")?.localPosition = new Vector3(0f, -100f, 0f);

        GameObject plantPanel = Instances.GlobalPanels.GetPanel("almanac").transform.Find("Canvas/Layout/Center/Panel/PlantPanel").gameObject;
        _panel = UnityEngine.Object.Instantiate(plantPanel, VsPanels.transform);
        _panel.name = "ArenaPanel";
        GameObject viewPlantsButton = _panel.transform.Find("ViewPlantsButton").gameObject;
        if (viewPlantsButton != null)
        {
            UnityEngine.Object.Destroy(viewPlantsButton);
        }

        _panel.transform.localPosition = new Vector3(0f, 780f, 0f);
        _panel.transform.localScale = Vector3.one * 0.5f;
        _panel.transform.SetSiblingIndex(2);

        var previewObj = _panel.transform.Find("Sunflower").gameObject;
        previewObj.RemoveComponent<RawImage>(true);
        _preview = previewObj.AddComponent<Image>();
        _preview.gameObject.name = "ArenaPreview";
        _preview.transform.localPosition = new Vector3(-14f, 5f, 0f);
        _preview.transform.localScale = new Vector3(3.3f, 2f, 2f);

        if (NetLobby.AmLobbyHost())
        {
            var forward = CreateButton(VsSideChooser, "-->", () =>
            {
                NetLobby.LobbyData.Arena = NetLobby.LobbyData.Arena.Next();
            });
            forward.transform.localPosition = new Vector3(110f, -390f, 0f);
            forward.transform.localScale = Vector3.one * 0.8f;

            var back = CreateButton(VsSideChooser, "<--", () =>
            {
                NetLobby.LobbyData.Arena = NetLobby.LobbyData.Arena.Previous();
            });
            back.transform.localPosition = new Vector3(-640f, -390f, 0f);
            back.transform.localScale = Vector3.one * 0.8f;

            SetupNavigation(VsSideChooser, forward, back);
        }

        SetPreview(VersusState.Arena);
    }

    /// <summary>
    /// Creates a navigation button with specified text and click action.
    /// </summary>
    /// <param name="VsSideChooser">The PanelView component used as a reference for button prefab.</param>
    /// <param name="name">The text to display on the button (typically arrow symbols like "-->" or "&lt;--").</param>
    /// <param name="action">The action to execute when the button is clicked.</param>
    /// <returns>The created Button component, or null if creation failed.</returns>
    private static Button CreateButton(PanelView VsSideChooser, string name, Action action)
    {
        var prefab = VsSideChooser.transform.Find("Canvas/Layout/Center/Panel/SelectionSets/QuickPlay");
        var button = UnityEngine.Object.Instantiate(prefab, _panel.transform)?.GetComponent<Button>();
        if (button != null)
        {
            var text = button.transform.Find("ButtonText")?.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.fontSizeMax = 150;
                text.SetText(name);
            }
            button.gameObject.name = "Button";
            button.gameObject.RemoveComponent<ButtonTransition>();
            button.onClick = new();
            button.onClick.AddListener(action);
        }
        return button;
    }

    /// <summary>
    /// Configures keyboard/controller navigation between the arena selection buttons and existing UI buttons.
    /// </summary>
    /// <param name="VsSideChooser">The PanelView component containing the selection sets.</param>
    /// <param name="forward">The forward navigation button (typically "-->" arrow).</param>
    /// <param name="back">The back navigation button (typically "&lt;--" arrow).</param>
    private static void SetupNavigation(PanelView VsSideChooser, Button forward, Button back)
    {
        Button[] others = VsSideChooser.transform.Find("Canvas/Layout/Center/Panel/SelectionSets").GetComponentsInChildren<Button>();

        if (others.Length < 4) return;

        Navigation forwardNav = forward.navigation;
        forwardNav.selectOnLeft = back;
        forwardNav.selectOnDown = others[2];
        forward.navigation = forwardNav;

        Navigation backNav = back.navigation;
        backNav.selectOnRight = forward;
        backNav.selectOnDown = others[1];
        back.navigation = backNav;

        for (int i = 0; i < others.Length; i++)
        {
            Button button = others[i];
            Navigation buttonNav = button.navigation;

            if (i < 2)
            {
                buttonNav.selectOnUp = back;
            }
            else
            {
                buttonNav.selectOnUp = forward;
            }

            button.navigation = buttonNav;
        }
    }

    /// <summary>
    /// Updates the preview image based on the specified arena type.
    /// </summary>
    /// <param name="arenaType">The arena type (Day or Night) to display a preview for.</param>
    internal static void SetPreview(ArenaTypes arenaType)
    {
        if (_panel == null || _preview == null) return;

        var arena = GetArenaLevelEntryData(arenaType);
        if (arena != null)
        {
            SetPreviewFromEntryData(arena);
        }
    }

    /// <summary>
    /// Gets the level entry data for the arena type.
    /// </summary>
    /// <param name="arenaType">The arena type (Day or Night) to display a preview for.</param>
    /// <returns>The LevelEntryData of the ArenaType</returns>
    internal static LevelEntryData GetArenaLevelEntryData(ArenaTypes arenaType)
    {
        string arenaName = string.Empty;
        switch (arenaType)
        {
            case ArenaTypes.Day:
                arenaName = "Level-AdventureArea1Level2";
                break;
            case ArenaTypes.Night:
                arenaName = "Level-AdventureArea2Level2";
                break;
        }

        return LevelEntries.GetLevel(arenaName);
    }

    /// <summary>
    /// Sets the preview sprite from a LevelEntryData object.
    /// </summary>
    /// <param name="levelEntryData">The level entry data containing the thumbnail sprite to display.</param>
    private static void SetPreviewFromEntryData(LevelEntryData levelEntryData)
    {
        Sprite sprite = levelEntryData.EntryThumbnail.Asset.Cast<Sprite>();
        _preview.sprite = sprite;
    }
}
using Il2CppReloaded.Data;
using Il2CppTekly.PanelViews;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace ReplantedOnline.Modules.Panel;

internal static class StageSelectorPanel
{
    private static GameObject _panel;
    private static Image _preview;

    internal static void Create(PanelView VsSideChooser)
    {
        if (_panel != null) return;

        var VsPanels = VsSideChooser.transform.Find("Canvas/Layout/Center").gameObject;
        VsPanels.transform.Find("Panel")?.localPosition = new Vector3(0f, -100f, 0f);

        GameObject plantPanel = Instances.GlobalPanels.GetPanel("almanac").transform.Find("Canvas/Layout/Center/Panel/PlantPanel").gameObject;
        _panel = UnityEngine.Object.Instantiate(plantPanel, VsPanels.transform);
        _panel.name = "StagePanel";
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
        _preview.gameObject.name = "StagePreview";
        _preview.transform.localPosition = new Vector3(-14f, 5f, 0f);
        _preview.transform.localScale = new Vector3(3.3f, 2f, 2f);

        SetPreview();
    }

    internal static void SetPreview()
    {
        LevelEntryData arena1 = Resources.FindObjectsOfTypeAll<LevelEntryData>().FirstOrDefault(data => data.name == "Level-AdventureArea1Level2");
        if (arena1 != null)
        {
            SetPreviewFromEntryData(arena1);
        }
    }

    private static void SetPreviewFromEntryData(LevelEntryData levelEntryData)
    {
        Sprite sprite = levelEntryData.EntryThumbnail.Asset.Cast<Sprite>();
        _preview.sprite = sprite;
    }
}

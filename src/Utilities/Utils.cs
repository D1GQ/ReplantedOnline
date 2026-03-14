using Il2CppTekly.PanelViews;

namespace ReplantedOnline.Utilities;

internal static class Utils
{
    internal static PanelView GetPanel(this PanelViewContainer panelViewContainer, string panelId)
    {
        foreach (var panel in panelViewContainer.m_panels)
        {
            if (panel.Id != panelId) continue;
            return panel;
        }

        return null;
    }
}

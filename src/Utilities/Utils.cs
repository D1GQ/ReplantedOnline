using Il2CppTekly.PanelViews;
using ReplantedOnline.Enums;

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

    /// <summary>
    /// Gets the opposite team for a given player team.
    /// </summary>
    /// <param name="team">The player team to get the opposite of.</param>
    /// <returns>
    /// The opposite team:
    /// <list type="bullet">
    /// <item><description>Plants → Zombies</description></item>
    /// <item><description>Zombies → Plants</description></item>
    /// <item><description>Any other value → None</description></item>
    /// </list>
    /// </returns>
    internal static PlayerTeam GetOppositeTeam(PlayerTeam team)
    {
        switch (team)
        {
            case PlayerTeam.Plants:
                return PlayerTeam.Zombies;
            case PlayerTeam.Zombies:
                return PlayerTeam.Plants;
            default:
                return PlayerTeam.None;
        }
    }
}

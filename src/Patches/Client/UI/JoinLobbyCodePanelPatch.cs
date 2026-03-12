using HarmonyLib;
using Il2CppTekly.PanelViews;
using ReplantedOnline.Modules.Panels;

namespace ReplantedOnline.Patches.Client.UI;

[HarmonyPatch]
internal static class JoinLobbyCodePanelPatch
{
    [HarmonyPatch(typeof(PanelViewContainer), nameof(PanelViewContainer.Awake))]
    [HarmonyPostfix]
    private static void PanelViewContainer_Awake_Postfix(PanelViewContainer __instance)
    {
        // Check if this is the frontend panels container
        if (__instance.name == "FrontendPanels")
        {
            // Find the existing users rename panel to use as a template
            var usersRenamePanel = __instance.m_panels.FirstOrDefault(p => p.Id == "usersRename");
            if (usersRenamePanel != null)
            {
                LobbyCodePanel.Init(usersRenamePanel);
            }
        }
    }
}
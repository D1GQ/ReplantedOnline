using HarmonyLib;
using Il2CppReloaded.DataModels;
using Il2CppReloaded.TreeStateActivities;
using Il2CppSource.DataModels;
using Il2CppTekly.Extensions.DataProviders;
using Il2CppTekly.PanelViews;
using Il2CppTekly.TreeState;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;

namespace ReplantedOnline.Patches;

[HarmonyPatch]
internal static class InstanceWrapperPatch
{
    [HarmonyPatch(typeof(UiDataProviderActivity), nameof(UiDataProviderActivity.LoadingStarted))]
    [HarmonyPostfix]
    private static void UiDataProviderActivity_Postfix(UiDataProviderActivity __instance)
    {
        // Only capture the data provider for the main gameplay activity
        if (__instance.gameObject.name == "GameplayActivity")
        {
            // Extract the GameplayDataProvider from the activity's providers
            GameplayDataProvider dataProvider = __instance.m_providers.First().Cast<GameplayDataProvider>();
            if (dataProvider != null)
            {
                if (NetLobby.AmInLobby())
                {
                    dataProvider.m_gameplayDataModel.m_player2DataModel.m_isEnabled.m_value = true;
                }

                InstanceWrapper<GameplayDataProvider>.Instance = dataProvider;
                InstanceWrapper<VersusDataModel>.Instance = dataProvider.m_gameplayDataModel.m_versusDataModel;
            }
        }
    }

    [HarmonyPatch(typeof(GameplayActivity), nameof(GameplayActivity.Awake))]
    [HarmonyPostfix]
    private static void GameplayActivity_Postfix(GameplayActivity __instance)
    {
        InstanceWrapper<GameplayActivity>.Instance = __instance;
    }

    [HarmonyPatch(typeof(TreeState), nameof(TreeState.Awake))]
    [HarmonyPostfix]
    private static void TreeStateActivity_Postfix(TreeState __instance)
    {
        if (__instance.gameObject.name == "GameBoot")
        {
            var dataServiceActivity = __instance.GetComponentInChildren<DataServiceActivity>(true);
            InstanceWrapper<DataServiceActivity>.Instance = dataServiceActivity;
        }
    }

    [HarmonyPatch(typeof(PanelViewContainer), nameof(PanelViewContainer.Awake))]
    [HarmonyPostfix]
    private static void PanelViewContainer_Postfix(PanelViewContainer __instance)
    {
        if (__instance.name == "GlobalPanels(Clone)")
        {
            Instances.GlobalPanels = __instance;
            ReplantedOnlinePopup.Init(__instance);
        }
    }
}
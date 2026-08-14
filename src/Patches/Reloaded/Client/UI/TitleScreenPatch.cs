using HarmonyLib;
using Il2CppSource.DataModels;
using Il2CppTekly.PanelViews;
using ReplantedOnline.Modules.Unity;
using ReplantedOnline.Network.Github;
using ReplantedOnline.Utilities.Unity;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ReplantedOnline.Patches.Reloaded.Client.UI;

[HarmonyPatch]
internal static class TitleScreenPatch
{
    [HarmonyPatch(typeof(PanelViewContainer), nameof(PanelViewContainer.Awake))]
    [HarmonyPostfix]
    private static void PanelViewContainer_Awake_Postfix(PanelViewContainer __instance)
    {
        if (__instance.name == "SplashScreenPanel(Clone)")
        {
            var splash = __instance.transform.Find("Splash1");
            if (splash != null)
            {
                var logo = splash.Find("Canvas/PvZ_Logo")?.GetComponentInChildren<Image>(true); ;
                var screen = splash.Find("Canvas/TitleScreen")?.GetComponentInChildren<Image>(true);
                if (logo != null && screen != null)
                {
                    logo.gameObject.DestroyAllImageLocalizers();
                    UnityEngine.Object.Destroy(logo);
                    screen.gameObject.DestroyAllImageLocalizers();
                    screen.sprite = ReplantedOnlineMod.Assets.Sprites.PromoCompressed.Asset;
                }

                var loadingRect = splash.Find("Canvas/LoadBar/LoadBarAnimationParent")?.GetComponentInChildren<RectTransform>(true);
                if (loadingRect != null)
                {
                    loadingRect.anchoredPosition3D = new(0f, -20f, 0f);
                    loadingRect.localScale = new(0.8f, 0.8f, 0.8f);
                }
            }
        }
    }

    [HarmonyPatch(typeof(LoadingDataModel), nameof(LoadingDataModel.OnTick))]
    [HarmonyPrefix]
    private static bool LoadingDataModel_OnTick_Prefix(LoadingDataModel __instance)
    {
        if (SplashScreen.isFinished)
        {
            __instance.m_curLoadTime += Time.deltaTime;

            float dataProgress = __instance.m_dataService.LoadProgress;
            float preloadProgress = __instance.m_preloadService.Progress;
            float githubProgress = MonoSingleton<GithubAPI>.Instance.Progress;

            float totalProgress = (dataProgress + preloadProgress + githubProgress) / 3f;
            totalProgress = Mathf.Min(totalProgress, 1f);

            bool isLoadingComplete = __instance.m_dataService.IsReady &&
                                    !__instance.m_preloadService.IsLoading &&
                                    MonoSingleton<GithubAPI>.Instance.IsReady &&
                                    __instance.m_curLoadTime >= __instance.m_minLoadingTime;

            __instance.m_isLoading.Value = !isLoadingComplete;
            __instance.m_loadingButton.IsInteractable = isLoadingComplete;
            __instance.m_currentProgress.Value = Mathf.Lerp(0f, totalProgress, __instance.m_curLoadTime / __instance.m_minLoadingTime);
        }

        return false;
    }
}
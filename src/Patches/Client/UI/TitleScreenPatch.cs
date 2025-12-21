using HarmonyLib;
using Il2CppTekly.PanelViews;
using ReplantedOnline.Helper;
using UnityEngine;
using UnityEngine.UI;

namespace ReplantedOnline.Patches.Client.UI;

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
                    screen.sprite = Utils.LoadSprite("ReplantedOnline.Resources.Images.PVZR-Online-Promo-Logo.png");
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
}
using HarmonyLib;
using Il2CppReloaded.Binders;
using Il2CppTMPro;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.MonoScripts.Modded;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Utilities.Il2Cpp;
using ReplantedOnline.Utilities.Modded;
using UnityEngine;
using UnityEngine.UI;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.UI;

[HarmonyPatch]
internal static class VersusHudPatch
{
    private static ContentSizeFitter? plantHud;
    private static ContentSizeFitter? zombieHud;

    [HarmonyPatch(typeof(ContentSizeFitter), nameof(ContentSizeFitter.OnEnable))]
    [HarmonyPostfix]
    private static void ContentSizeFitter_OnEnable_Postfix(ContentSizeFitter __instance)
    {
        if (ReloadedLobby.AmInLobby())
        {
            if (__instance.name == "TopLeftLayout")
            {
                plantHud = __instance;
            }

            if (__instance.name == "VersusBank")
            {
                zombieHud = __instance;
            }
        }
    }

    internal static void SetHuds()
    {
        if (plantHud == null) return;
        if (zombieHud == null) return;

        // Set opponent money
        Instances.GameplayActivity.Board.mSunMoney.OpponentItem().Amount = 9999;

        // Scale HUDs
        if (plantHud.transform.Il2CppTryCast<RectTransform>(out var plantHudRect))
        {
            plantHudRect.anchoredPosition = new(35, -35);
            plantHudRect.localScale = new Vector3(0.9f, 0.9f, 1f);
        }
        if (zombieHud.transform.Il2CppTryCast<RectTransform>(out var zombieHudRect))
        {
            zombieHudRect.anchoredPosition = new(-35, -35);
            zombieHudRect.localScale = new Vector3(0.9f, 0.9f, 1f);
        }

        // Set up Timer Container
        var mainHUDLayout = plantHud.transform.parent;
        if (mainHUDLayout != null)
        {
            if (mainHUDLayout.Il2CppTryCast<RectTransform>(out var mainHUDLayoutRect))
            {
                var shovelContainer = plantHud.transform.Find("ShovelContainer");
                if (shovelContainer != null)
                {
                    if (shovelContainer.Il2CppTryCast<RectTransform>(out var shovelContainerRect))
                    {
                        var textPrefab = mainHUDLayout.Find("TreeOfWisdomSize")?.GetComponent<TextMeshProUGUI>();
                        if (textPrefab != null)
                        {
                            TimerPanel.Initialize(mainHUDLayoutRect, textPrefab, shovelContainerRect);
                        }
                    }
                }
            }
        }

        // Reposition pause button
        var menuButtonContainer = plantHud.transform.parent?.Find("MenuButtonVisiblityContainer");
        if (menuButtonContainer != null)
        {
            if (menuButtonContainer.Find("Nested_VS").Il2CppTryCast<RectTransform>(out var menuButtonContainerRect))
            {
                menuButtonContainerRect.anchoredPosition = new Vector2(-35f, 0f);
                menuButtonContainerRect.localScale = new Vector3(0.9f, 0.9f, 1f);
            }
        }

        // Hide opponent currency
        if (VersusState.AmZombieSide)
        {
            var binder = plantHud.transform.Find("SeedBankContainer/SeedBank/SunAmount_Background")?.GetComponentInChildren<NumberLabelBinder>(true);
            SetUnknownCount(binder);
        }
        else
        {
            var binder = zombieHud.transform.Find("VersusBankContainer/P_VsZombiePacks_Layout/Seedpacks_Background")?.GetComponentInChildren<NumberLabelBinder>(true);
            SetUnknownCount(binder);
        }
    }

    private static void SetUnknownCount(NumberLabelBinder? binder)
    {
        if (binder == null)
            return;

        binder.m_formatString = "???";
        binder.BindNumber(0);
    }
}

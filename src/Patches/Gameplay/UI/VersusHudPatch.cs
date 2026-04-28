using HarmonyLib;
using Il2CppReloaded.Binders;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace ReplantedOnline.Patches.Gameplay.UI;

[HarmonyPatch]
internal static class VersusHudPatch
{
    private static ContentSizeFitter plantHud;
    private static ContentSizeFitter zombieHud;

    [HarmonyPatch(typeof(ContentSizeFitter), nameof(ContentSizeFitter.OnEnable))]
    [HarmonyPostfix]
    private static void ContentSizeFitter_OnEnable_Postfix(ContentSizeFitter __instance)
    {
        if (ReplantedLobby.AmInLobby())
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
        if (plantHud.transform.Il2CppTryCast<RectTransform>(out var rect))
        {
            rect.anchoredPosition = new(35, -35);
            rect.localScale = new Vector3(0.9f, 0.9f, 1f);
        }
        if (zombieHud.transform.Il2CppTryCast<RectTransform>(out var rect2))
        {
            rect2.anchoredPosition = new(-35, -35);
            rect2.localScale = new Vector3(0.9f, 0.9f, 1f);
        }

        var menuButtonContainer = plantHud.transform.parent?.Find("MenuButtonVisiblityContainer");
        if (menuButtonContainer == null) return;

        if (menuButtonContainer.Find("Nested_VS").Il2CppTryCast<RectTransform>(out var rect3))
        {
            rect3.anchoredPosition = new Vector2(-35f, 0f);
            rect3.localScale = new Vector3(0.9f, 0.9f, 1f);
        }

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

    private static void SetUnknownCount(NumberLabelBinder binder)
    {
        if (binder == null)
            return;

        binder.m_formatString = "???";
        binder.BindNumber(0);
    }
}

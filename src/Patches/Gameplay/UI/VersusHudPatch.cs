using HarmonyLib;
using Il2CppReloaded.Binders;
using Il2CppReloaded.Gameplay;
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

    // Hide opponents hud
    internal static void SetHuds()
    {
        Instances.GameplayActivity.Board.mSunMoney.OpponentItem().Amount = 9999;

        plantHud?.transform?.Cast<RectTransform>().localScale = new(0.9f, 0.9f, 1f);
        zombieHud?.transform?.Cast<RectTransform>().localScale = new(0.9f, 0.9f, 1f);

        if (VersusState.AmZombieSide)
        {
            if (VersusState.SelectionSet == SelectionSet.Random)
            {
                plantHud?.gameObject?.SetActive(false);
            }
            else
            {
                var numberLabelBinder = plantHud?.transform?.Find("SeedBankContainer/SeedBank/SunAmount_Background")?.GetComponentInChildren<NumberLabelBinder>(true);
                numberLabelBinder.m_formatString = "???";
                numberLabelBinder.BindNumber(0);
            }
        }
        else
        {
            var menuButtonVisiblityContainer = plantHud?.transform?.parent?.Find("MenuButtonVisiblityContainer");
            if (menuButtonVisiblityContainer != null)
            {
                if (VersusState.SelectionSet == SelectionSet.Random)
                {
                    menuButtonVisiblityContainer.Find("Nested_VS")?.gameObject?.SetActive(false);
                    menuButtonVisiblityContainer.Find("Nested_NotVS")?.gameObject?.SetActive(true);
                    zombieHud?.gameObject?.SetActive(false);
                }
                else
                {
                    menuButtonVisiblityContainer.Find("Nested_VS")?.transform?.localPosition = new(0f, 35f, 0f);
                    var numberLabelBinder = zombieHud?.transform?.Find("VersusBankContainer/P_VsZombiePacks_Layout/Seedpacks_Background")?.GetComponentInChildren<NumberLabelBinder>(true);
                    numberLabelBinder.m_formatString = "???";
                    numberLabelBinder.BindNumber(0);
                }
            }
        }
    }
}

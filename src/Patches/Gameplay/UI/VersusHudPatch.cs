using HarmonyLib;
using Il2CppReloaded.Binders;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
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
        if (NetLobby.AmInLobby())
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
            if (VersusState.SelectionSet == SelectionSet.Random)
            {
                plantHud?.transform?.parent?.Find("MenuButtonVisiblityContainer")?.transform?.position += new Vector3(0f, 350f, 0f);
                zombieHud?.gameObject?.SetActive(false);
            }
            else
            {
                var numberLabelBinder = zombieHud?.transform?.Find("VersusBankContainer/P_VsZombiePacks_Layout/Seedpacks_Background")?.GetComponentInChildren<NumberLabelBinder>(true);
                numberLabelBinder.m_formatString = "???";
                numberLabelBinder.BindNumber(0);
            }
        }
    }
}

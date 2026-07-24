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
        if (VersusState.AmPlantSide)
        {
            var binder = zombieHud.transform.Find("VersusBankContainer/P_VsZombiePacks_Layout/Seedpacks_Background")?.GetComponentInChildren<NumberLabelBinder>(true);
            SetUnknownCount(binder);
        }
        else if (VersusState.AmZombieSide)
        {
            var binder = plantHud.transform.Find("SeedBankContainer/SeedBank/SunAmount_Background")?.GetComponentInChildren<NumberLabelBinder>(true);
            SetUnknownCount(binder);
        }
        else
        {
            var plantBinder = plantHud.transform.Find("SeedBankContainer/SeedBank/SunAmount_Background")?.GetComponentInChildren<NumberLabelBinder>(true);
            var zombieBinder = zombieHud.transform.Find("VersusBankContainer/P_VsZombiePacks_Layout/Seedpacks_Background")?.GetComponentInChildren<NumberLabelBinder>(true);
            SetUnknownCount(plantBinder);
            SetUnknownCount(zombieBinder);
        }

        SetupSeedPacketCross();
    }

    private static void SetupSeedPacketCross()
    {
        if (plantHud == null || zombieHud == null)
            return;

        var plantCross = plantHud.transform.Find("SeedBankContainer/SeedBank/SeedPacks_Layout/Cross");
        var plantSeedpacketPrefab = plantHud.transform.Find("SeedBankContainer/SeedBank/SeedPacks_Layout/P_GamePlay_SeedChooser_Item");
        if (plantCross != null && plantSeedpacketPrefab != null)
        {
            plantCross.SetParent(plantSeedpacketPrefab);
            plantCross.Cast<RectTransform>().anchoredPosition3D = new(100f, -125f, 0f);
        }

        var zombieCross = zombieHud.transform.Find("VersusBankContainer/P_VsZombiePacks_Layout/Cross");
        var zombieSeedpacketPrefab = zombieHud.transform.Find("VersusBankContainer/P_VsZombiePacks_Layout/P_GamePlay_SeedChooser_Item");
        if (zombieCross != null && zombieSeedpacketPrefab != null)
        {
            zombieCross.SetParent(zombieSeedpacketPrefab);
            zombieCross.Cast<RectTransform>().anchoredPosition3D = new(100f, -125f, 0f);
        }
    }

    private static void SetUnknownCount(NumberLabelBinder? binder)
    {
        if (binder == null)
            return;

        binder.m_formatString = "???";
        binder.BindNumber(0);
    }

    internal static GameObject? GetPlantSeedPacket(int index)
    {
        return plantHud?.transform.Find("SeedBankContainer/SeedBank/SeedPacks_Layout")?.GetChild(index + 2)?.gameObject;
    }

    internal static GameObject? GetZombieSeedPacket(int index)
    {
        return zombieHud?.transform.Find("VersusBankContainer/P_VsZombiePacks_Layout")?.GetChild(index + 2)?.gameObject;
    }
}

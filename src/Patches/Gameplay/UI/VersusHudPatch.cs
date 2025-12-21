using HarmonyLib;
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
        if (VersusState.AmZombieSide)
        {
            plantHud?.gameObject?.SetActive(false);
        }
        else
        {
            plantHud?.transform?.parent?.Find("MenuButtonVisiblityContainer")?.transform?.position += new Vector3(0f, 350f, 0f);
            zombieHud?.gameObject?.SetActive(false);
        }
    }
}

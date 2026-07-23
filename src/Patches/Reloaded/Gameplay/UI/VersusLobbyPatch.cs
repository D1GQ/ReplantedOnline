#pragma warning disable CS0162

using HarmonyLib;
using Il2CppSource.Controllers;
using Il2CppSource.DataModels;
using Il2CppSource.UI;
using Il2CppTekly.PanelViews;
using Il2CppTMPro;
using MelonLoader;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Managers.Reloaded;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded.Panel;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Network.Reloaded.Client.Routing;
using ReplantedOnline.Network.Reloaded.Client.Routing.Rpc;
using ReplantedOnline.Utilities.Unity;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ReplantedOnline.Patches.Reloaded.Gameplay.UI;

[HarmonyPatch]
internal static class VersusLobbyPatch
{
    private static GameObject? InteractableBlocker;
    private static GameObject? InteractableGamePad;
    private static GameObject? LobbyBackground;
    internal static PanelView? VsSideChooser;

    [HarmonyPatch(typeof(PanelViewContainer), nameof(PanelViewContainer.Awake))]
    [HarmonyPostfix]
    private static void PanelViewContainer_Awake_Postfix(PanelViewContainer __instance)
    {
        if (!ReloadedLobby.AmInLobby()) return;

        VsSideChooser = __instance.m_panels.FirstOrDefault(pan => pan.gameObject.name == "P_VsSideChooser");
        if (VsSideChooser == null) return;

        VsSideChooser.gameObject.DestroyAllTextLocalizers();

        InteractableBlocker = VsSideChooser.transform.Find("Canvas/Layout/Center/Panel/SelectionSets/DisableInteraction")?.gameObject;
        InteractableGamePad = VsSideChooser.transform.Find("Canvas/Layout/Center/Panel/SelectionSets/SelectionSets_SidesChosenNavLayer")?.gameObject;

        if (ReloadedLobby.AmLobbyHost())
        {
            SetupHostUI(VsSideChooser);
        }
        else
        {
            SetupClientUI(VsSideChooser);
        }

        ArenaSelectorPanel.Create(VsSideChooser);
        VersusLobbyManager.SetTextComps(VsSideChooser);
        VersusLobbyManager.UpdateSideVisuals();
    }

    private static void SetupHostUI(PanelView panelView)
    {
        panelView.SetVSButton("QuickPlay", () => NetworkManager.Rpc<StartGameRpc>.Singleton.Send(VersusGamemodeType.Quickplay));
        panelView.SetVsButtonTitle("QuickPlay", "Quick\nBattle");

        panelView.RemoveVSButton("Custom");

        panelView.SetVSButton("CustomAll", () => NetworkManager.Rpc<StartGameRpc>.Singleton.Send(VersusGamemodeType.Custom));
        panelView.SetVsButtonTitle("CustomAll", "Custom\nBattle");

        panelView.SetVSButton("Random", () => NetworkManager.Rpc<StartGameRpc>.Singleton.Send(VersusGamemodeType.Random));

        FixNavigation(panelView);
        panelView.transform.Find("Canvas/Layout/Center/Panel/ControllerBottom")?.gameObject?.SetActive(false);
    }

    private static void SetupClientUI(PanelView panelView)
    {
        panelView.RemoveSelectionButtons();
        InteractableBlocker?.transform?.localScale = new(10f, 10f, 10f);
        panelView.transform.Find("Canvas/Layout/Center/Panel/ControllerTop")?.gameObject?.SetActive(false);
        panelView.transform.Find("Canvas/Layout/Center/Panel/ControllerBottom")?.gameObject?.SetActive(false);
    }

    private static void FixNavigation(PanelView panelView)
    {
        var selectionSets = panelView.transform.Find("Canvas/Layout/Center/Panel/SelectionSets");
        if (selectionSets == null) return;

        Button[] buttons = selectionSets.GetComponentsInChildren<Button>();
        if (buttons.Length < 3) return;

        Button? quickPlay = buttons.FirstOrDefault(b => b.name.Contains("QuickPlay"));
        Button? customAll = buttons.FirstOrDefault(b => b.name.Contains("CustomAll"));

        if (quickPlay == null || customAll == null) return;

        var quickNav = quickPlay.navigation;
        quickNav.mode = Navigation.Mode.Explicit;
        quickNav.selectOnRight = customAll;
        quickPlay.navigation = quickNav;

        var customNav = customAll.navigation;
        customNav.mode = Navigation.Mode.Explicit;
        customNav.selectOnLeft = quickPlay;
        customAll.navigation = customNav;
    }

    [HarmonyPatch(typeof(BackgroundController), nameof(BackgroundController.Awake))]
    [HarmonyPostfix]
    private static void BackgroundController_Awake_Postfix(BackgroundController __instance)
    {
        if (!ReloadedLobby.AmInLobby() || VsSideChooser == null) return;

        if (LobbyBackground == null)
        {
            LobbyBackground = UnityEngine.Object.Instantiate(
                __instance.transform.Find("Sky RIP").gameObject,
                VsSideChooser.transform.parent
            );

            LobbyBackground.SetActive(true);

            foreach (var spriteRenderer in LobbyBackground.transform.GetComponentsInChildren<SpriteRenderer>())
            {
                spriteRenderer.sortingLayerID = 0;
            }

            LobbyBackground.transform.position = new Vector3(0f, 0f, -100f);
            LobbyBackground.transform.localScale = Vector3.one * 2f;

            var lowerClouds = UnityEngine.Object.Instantiate(
                LobbyBackground.transform.Find("RIP SkySprites Parent/Clouds_RIP"),
                LobbyBackground.transform.Find("RIP SkySprites Parent")
            );
            lowerClouds.transform.localPosition = new Vector3(0f, -900f, 0f);
        }
    }

    internal static void HideLobbyBackground() => LobbyBackground?.SetActive(false);

    internal static void SetButtonsInteractable(bool interactable)
    {
        if (InteractableBlocker == null || InteractableGamePad == null) return;

        InteractableBlocker.SetActive(!interactable);
        InteractableGamePad.SetActive(interactable);
    }

    private static void SetVSButton(this PanelView panelView, string name, Action callback)
    {
        MelonCoroutines.Start(CoSetVSButton(panelView, name, callback));
    }

    private static IEnumerator CoSetVSButton(PanelView panelView, string name, Action callback)
    {
        yield return new WaitForSeconds(0.5f);

        var button = panelView?.transform
            ?.Find($"Canvas/Layout/Center/Panel/SelectionSets/{name}")
            ?.GetComponentInChildren<Button>(true);

        if (button == null)
            yield break;

        button.onClick = new();
        button.onClick.AddListener(callback);

        var bt = button.GetComponentInChildren<ButtonTransition>(true);
        if (bt != null) UnityEngine.Object.Destroy(bt);
    }

    private static void SetVsButtonTitle(this PanelView panelView, string name, string title)
    {
        var textPro = panelView.transform.Find($"Canvas/Layout/Center/Panel/SelectionSets/{name}")?.GetComponentInChildren<TextMeshProUGUI>(true);
        textPro?.SetText(title);
    }

    private static void RemoveVSButton(this PanelView panelView, string name)
    {
        var button = panelView.transform.Find($"Canvas/Layout/Center/Panel/SelectionSets/{name}")?.gameObject;
        if (button != null) UnityEngine.Object.Destroy(button);
    }

    private static void RemoveSelectionButtons(this PanelView panelView)
    {
        var buttons = panelView.transform
            .Find("Canvas/Layout/Center/Panel/SelectionSets")
            ?.gameObject;

        if (buttons != null) UnityEngine.Object.Destroy(buttons);
    }

    [HarmonyPatch(typeof(VersusPlayerModel), nameof(VersusPlayerModel.Confirm))]
    [HarmonyPrefix]
    private static bool Confirm_Prefix(VersusPlayerModel __instance, ref bool __state)
    {
        __state = false;
        if (!ReloadedLobby.AmLobbyHost()) return false;

        if (!ReplantedOnlineMod.ModInfo.DEBUG)
        {
            if (!ReloadedLobby.LobbyData!.AllClientsReady() || ReloadedLobby.GetLobbyClientCount() < 2)
            {
                return false;
            }
        }

        __state = true;
        return true;
    }

    [HarmonyPatch(typeof(VersusPlayerModel), nameof(VersusPlayerModel.Confirm))]
    [HarmonyPostfix]
    private static void Confirm_Postfix(bool __state)
    {
        if (!__state) return;
        if (ReloadedLobby.LobbyData == null) return;

        if (Instances.GameplayActivity.VersusMode.PlantPlayerIndex == 0)
        {
            ReloadedLobby.LobbyData.HostTeam.Value = PlayerTeam.Plants;
        }
        else
        {
            ReloadedLobby.LobbyData.HostTeam.Value = PlayerTeam.Zombies;
        }
    }

    [HarmonyPatch(typeof(VersusPlayerModel), nameof(VersusPlayerModel.Cancel))]
    [HarmonyPrefix]
    private static bool Cancel_Prefix(ref bool __state)
    {
        __state = false;
        if (!ReloadedLobby.AmLobbyHost()) return false;

        __state = true;
        return true;
    }

    [HarmonyPatch(typeof(VersusPlayerModel), nameof(VersusPlayerModel.Cancel))]
    [HarmonyPostfix]
    private static void Cancel_Postfix(VersusPlayerModel __instance, bool __state)
    {
        if (!__state || ReloadedLobby.LobbyData == null) return;

        ReloadedLobby.LobbyData.HostTeam.Value = PlayerTeam.None;
    }
}
using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppSource.DataModels;
using Il2CppTekly.PanelViews;
using Il2CppTMPro;
using MelonLoader;
using ReplantedOnline.Enums;
using ReplantedOnline.Helper;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.RPC.Handlers;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ReplantedOnline.Patches.UI;

[HarmonyPatch]
internal static class LobbyPatch
{
    private static GameObject InteractableBlocker;
    private static GameObject InteractableGamePad;
    internal static PanelView VsSideChooser;

    [HarmonyPatch(typeof(PanelViewContainer), nameof(PanelViewContainer.Awake))]
    [HarmonyPostfix]
    private static void Awake_Postfix(PanelViewContainer __instance)
    {
        // Only modify UI if we're in an online lobby
        if (!NetLobby.AmInLobby()) return;

        // Find the VS side chooser panel
        VsSideChooser = __instance.m_panels.FirstOrDefault(pan => pan.gameObject.name == "P_VsSideChooser");
        if (VsSideChooser != null)
        {
            VsSideChooser.gameObject.DestroyAllTextLocalizers();

            InteractableBlocker = VsSideChooser.transform.Find($"Canvas/Layout/Center/Panel/SelectionSets/DisableInteraction")?.gameObject ?? null;
            InteractableGamePad = VsSideChooser.transform.Find($"Canvas/Layout/Center/Panel/SelectionSets/SelectionSets_SidesChosenNavLayer")?.gameObject ?? null;

            if (NetLobby.AmLobbyHost())
            {
                // Host gets all game mode options
                VsSideChooser.SetVSButton("QuickPlay", () =>
                {
                    StartGameHandler.Send(SelectionSet.QuickPlay);
                });
                VsSideChooser.SetVsButtonTitle("QuickPlay", "Quick\nBattle");

                VsSideChooser.SetVSButton("Custom", () =>
                {
                    ReplantedOnlinePopup.Show("Under Construction", "This game mode will be coming soon!");
                });
                VsSideChooser.SetVsButtonTitle("Custom", "Speed\nBattle");

                VsSideChooser.SetVSButton("CustomAll", () =>
                {
                    StartGameHandler.Send(SelectionSet.CustomAll);
                });
                VsSideChooser.SetVsButtonTitle("CustomAll", "Custom\nBattle");

                VsSideChooser.SetVSButton("Random", () =>
                {
                    StartGameHandler.Send(SelectionSet.Random);
                });

                VsSideChooser.transform.Find($"Canvas/Layout/Center/Panel/ControllerBottom")?.gameObject?.SetActive(false);
            }
            else
            {
                // Non-host players wait for host to choose
                VsSideChooser.RemoveSelectionButtons(); // Remove all selection buttons

                InteractableBlocker?.transform?.localScale = new(10f, 10f, 10f); // Block all input as host

                VsSideChooser.transform.Find($"Canvas/Layout/Center/Panel/ControllerTop")?.gameObject?.SetActive(false);
                VsSideChooser.transform.Find($"Canvas/Layout/Center/Panel/ControllerBottom")?.gameObject?.SetActive(false);
            }

            VersusManager.SetTextComps(VsSideChooser);
            VersusManager.UpdateSideVisuals();
        }
    }

    private static void SetVSButton(this PanelView panelView, string name, Action callback)
    {
        MelonCoroutines.Start(CoSetVSButton(panelView, name, callback));
    }

    private static IEnumerator CoSetVSButton(PanelView panelView, string name, Action callback)
    {
        yield return new WaitForSeconds(0.5f);
        var button = panelView?.transform?.Find($"Canvas/Layout/Center/Panel/SelectionSets/{name}")?.GetComponentInChildren<Button>(true);
        if (button != null)
        {
            button.onClick = new();
            button.onClick.AddListener(callback);
        }
    }

    private static void SetVsButtonTitle(this PanelView panelView, string name, string title)
    {
        var textPro = panelView.transform.Find($"Canvas/Layout/Center/Panel/SelectionSets/{name}")?.GetComponentInChildren<TextMeshProUGUI>(true);
        textPro?.SetText(title);
    }

    private static void RemoveVSButton(this PanelView panelView, string name)
    {
        // Remove specific game mode button
        var button = panelView.transform.Find($"Canvas/Layout/Center/Panel/SelectionSets/{name}")?.gameObject;
        if (button != null)
        {
            UnityEngine.Object.Destroy(button);
        }
    }

    private static void RemoveSelectionButtons(this PanelView panelView)
    {
        // Remove all game mode selection buttons (for non-host players)
        var buttons = panelView.transform.Find($"Canvas/Layout/Center/Panel/SelectionSets")?.gameObject;
        if (buttons != null)
        {
            UnityEngine.Object.Destroy(buttons);
        }
    }

    internal static void SetButtonsInteractable(bool interactable)
    {
        if (InteractableBlocker == null || InteractableGamePad == null) return;

        InteractableBlocker.SetActive(!interactable);
        InteractableGamePad.SetActive(interactable);
    }

    [HarmonyPatch(typeof(VersusPlayerModel), nameof(VersusPlayerModel.Confirm))]
    [HarmonyPostfix]
    private static void Confirm_Prefix(VersusPlayerModel __instance)
    {
        if (!NetLobby.AmLobbyHost()) return;

        if (Instances.GameplayActivity.VersusMode.PlantPlayerIndex == 0)
        {
            NetLobby.LobbyData.Networked.SetHostTeam(PlayerTeam.Plants);
        }
        else
        {
            NetLobby.LobbyData.Networked.SetHostTeam(PlayerTeam.Zombies);
        }
    }

    [HarmonyPatch(typeof(VersusPlayerModel), nameof(VersusPlayerModel.Cancel))]
    [HarmonyPostfix]
    private static void Cancel_Prefix(VersusPlayerModel __instance)
    {
        if (!NetLobby.AmLobbyHost()) return;

        NetLobby.LobbyData.Networked.SetHostTeam(PlayerTeam.None);
    }
}
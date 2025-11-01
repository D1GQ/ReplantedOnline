using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppTekly.PanelViews;
using MelonLoader;
using ReplantedOnline.Items.Enums;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.RPC.Handlers;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ReplantedOnline.Patches.UI;

[HarmonyPatch]
internal static class VsSideChoosererPatch
{
    private static GameObject InteractableBlocker;
    private static PanelView VsSideChooser;

    [HarmonyPatch(typeof(PanelViewContainer), nameof(PanelViewContainer.Awake))]
    [HarmonyPostfix]
    internal static void Awake_Postfix(PanelViewContainer __instance)
    {
        // Only modify UI if we're in an online lobby
        if (!NetLobby.AmInLobby()) return;

        // Find the VS side chooser panel
        VsSideChooser = __instance.m_panels.FirstOrDefault(pan => pan.gameObject.name == "P_VsSideChooser");
        if (VsSideChooser != null)
        {
            InteractableBlocker = VsSideChooser.transform.Find($"Canvas/Layout/Center/Panel/SelectionSets/DisableInteraction")?.gameObject ?? null;

            if (NetLobby.AmLobbyHost())
            {
                // Host gets all game mode options
                VsSideChooser.RemoveVSButton("Custom"); // Remove original custom button
                VsSideChooser.SetVSButton("QuickPlay", () =>
                {
                    StartGameHandler.Send(SelectionSet.QuickPlay); // Start quick play mode
                });
                VsSideChooser.SetVSButton("CustomAll", () =>
                {
                    StartGameHandler.Send(SelectionSet.CustomAll); // Start custom all mode
                });
                VsSideChooser.SetVSButton("Random", () =>
                {
                    StartGameHandler.Send(SelectionSet.Random); // Start random mode
                });

                VsSideChooser.AddSideButton("SidePlants", () =>
                {
                    if (NetLobby.LobbyData.LastGameState != GameState.HostChoosePlants)
                    {
                        UpdateGameStateHandler.Send(GameState.HostChoosePlants);
                    }
                });

                VsSideChooser.AddSideButton("SideZombies", () =>
                {
                    if (NetLobby.LobbyData.LastGameState != GameState.HostChooseZombie)
                    {
                        UpdateGameStateHandler.Send(GameState.HostChooseZombie);
                    }
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
        }
    }

    private static void SetVSButton(this PanelView panelView, string name, Action callback)
    {
        // Wait for UI to initialize then set up button
        MelonCoroutines.Start(CoSetVSButton(panelView, name, callback));
    }

    private static IEnumerator CoSetVSButton(this PanelView panelView, string name, Action callback)
    {
        yield return new WaitForSeconds(1f); // Wait for UI to load
        var button = panelView.transform.Find($"Canvas/Layout/Center/Panel/SelectionSets/{name}")?.GetComponentInChildren<Button>();
        if (button != null)
        {
            button.onClick = new();
            button.onClick.AddListener(callback); // Attach our online callback
        }
    }

    private static void AddSideButton(this PanelView panelView, string name, Action callback)
    {
        var button = panelView.transform.Find($"Canvas/Layout/Center/Panel/{name}")?.GetComponentInChildren<Button>();
        button?.onClick.AddListener(callback); // Attach our online callback
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

    internal static void SetInteractable(bool interactable)
    {
        InteractableBlocker?.SetActive(!interactable);
    }
}
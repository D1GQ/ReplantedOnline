using Il2Cpp;
using Il2CppSource.Utils;
using Il2CppTekly.TreeState;
using MelonLoader;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Panel;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Modules;

/// <summary>
/// Provides utility methods for scene transitions in ReplantedOnline.
/// </summary>
internal static class Transitions
{
    /// <summary>
    /// Transitions to the main menu scene.
    /// </summary>
    internal static void ToMainMenu(Action callback = null)
    {
        StateTransitionUtils.Transition("Frontend");
        if (callback != null)
        {
            MelonCoroutines.Start(CoWaitForTransition(callback, "Frontend"));
        }
    }

    /// <summary>
    /// Transitions to the Versus mode scene for online multiplayer matches.
    /// </summary>
    internal static void ToVersus(Action callback = null)
    {
        var level = LevelEntries.GetLevel("Level-Versus");
        level.GetGameplayService().SetCurrentLevelData(level);
        LevelEntries.ResetVersusArena();
        StateTransitionUtils.Transition("Gameplay");
        StateTransitionUtils.Transition("Versus");
        if (callback != null)
        {
            MelonCoroutines.Start(CoWaitForTransition(callback, "Versus"));
        }
    }

    /// <summary>
    /// Transitions to the lawn/board
    /// </summary>
    internal static void ToGameplay(Action callback = null)
    {
        StateTransitionUtils.Transition("Gameplay");
        if (callback != null)
        {
            MelonCoroutines.Start(CoWaitForTransition(callback, "Gameplay"));
        }
    }

    /// <summary>
    /// Transitions to the seed selection scene for choosing plants and zombies.
    /// </summary>
    internal static void ToChooseSeeds(Action callback = null)
    {
        StateTransitionUtils.Transition("ChooseSeeds");
        if (callback != null)
        {
            MelonCoroutines.Start(CoWaitForTransition(callback, "ChooseSeeds"));
        }
    }

    /// <summary>
    /// Transitions to an loading state
    /// </summary>
    internal static void ToGameEnd(Action callback = null)
    {
        StateTransitionUtils.Transition("Win");
        if (callback != null)
        {
            MelonCoroutines.Start(CoWaitForTransition(callback, "Win"));
        }
    }

    /// <summary>
    /// Sets the loading state
    /// </summary>
    internal static void SetLoading()
    {
        Instances.GlobalPanels.GetPanel("loadingScrim").gameObject.SetActive(true);
        SetFade();
        MelonCoroutines.Start(CoroutineUtils.WaitForCondition(() =>
        {
            return !Instances.GlobalPanels.GetPanel("loadingScrim").gameObject.activeInHierarchy;
        }, FadeIn));
    }

    /// <summary>
    /// Sets the FadePanel active and color.
    /// </summary>
    /// <param name="color">The color to use, Black by default.</param>
    internal static void SetFade(Color? color = null)
    {
        color ??= Color.black;
        var fadePanel = Instances.GlobalPanels.transform.Find("FadePanel").GetComponent<FadePanel>();
        fadePanel.mFadeIn = -1;
        fadePanel.SetColor(color.Value);
    }

    /// <summary>
    /// Starts FadePanel fading.
    /// </summary>
    internal static void FadeIn()
    {
        var fadePanel = Instances.GlobalPanels.transform.Find("FadePanel").GetComponent<FadePanel>();
        fadePanel.FadeInIfOut();
    }

    private static IEnumerator CoWaitForTransition(Action callback, string transitionName, float timeout = 10f)
    {
        float startTime = Time.time;

        while (StateTransitionUtils.s_treeStateManager?.Active == null)
        {
            if (Time.time - startTime > timeout)
            {
                ReplantedOnlineMod.Logger.Error(typeof(Transitions), $"Timeout waiting for transition '{transitionName}' to start");
                if (ReplantedLobby.AmInLobby())
                {
                    ReplantedLobby.LeaveLobby(() =>
                    {
                        CustomPopupPanel.Show("Disconnected", $"`{transitionName}` Transition has timed out!");
                    });
                }
                yield break;
            }
            yield return null;
        }

        while (StateTransitionUtils.s_treeStateManager.Active?.Name != transitionName ||
               !IsStateFullyLoaded(StateTransitionUtils.s_treeStateManager.Active))
        {
            if (Time.time - startTime > timeout)
            {
                ReplantedOnlineMod.Logger.Error(typeof(Transitions), $"Timeout waiting for transition '{transitionName}' to complete");
                if (ReplantedLobby.AmInLobby())
                {
                    ReplantedLobby.LeaveLobby(() =>
                    {
                        CustomPopupPanel.Show("Disconnected", $"`{transitionName}` Transition has timed out!");
                    });
                }
                yield break;
            }

            yield return null;
        }

        callback?.Invoke();
    }

    private static bool IsStateFullyLoaded(TreeState state)
    {
        if (state == null) return false;
        return state.IsDoneLoading();
    }
}
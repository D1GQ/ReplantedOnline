using Il2CppSource.Utils;
using MelonLoader;
using ReplantedOnline.Helper;
using System.Collections;

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
        Instances.GlobalPanels.GetPanel("loadingScrim")?.gameObject?.SetActive(true);
    }

    private static IEnumerator CoWaitForTransition(Action callback, string TransitionName)
    {
        while (StateTransitionUtils.s_treeStateManager.Active?.Name != TransitionName ||
               (StateTransitionUtils.s_treeStateManager.Active?.Name == TransitionName &&
                !StateTransitionUtils.s_treeStateManager.Active.IsDoneLoading()))
        {
            yield return null;
        }

        callback();
    }
}
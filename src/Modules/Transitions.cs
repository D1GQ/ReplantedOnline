using Il2CppSource.Utils;
using ReplantedOnline.Helper;

namespace ReplantedOnline.Modules;

/// <summary>
/// Provides utility methods for scene transitions in ReplantedOnline.
/// </summary>
internal class Transitions
{
    /// <summary>
    /// Transitions to the main menu scene.
    /// </summary>
    internal static void ToMainMenu() => StateTransitionUtils.Transition("Frontend");

    /// <summary>
    /// Transitions to the Versus mode scene for online multiplayer matches.
    /// </summary>
    internal static void ToVersus()
    {
        var level = LevelEntries.GetLevel("VS. Mode");
        level.GetGameplayService().SetCurrentLevelData(level);
        StateTransitionUtils.Transition("Gameplay");
        StateTransitionUtils.Transition("Versus");
    }

    /// <summary>
    /// Transitions to the lawn/board
    /// </summary>
    internal static void ToGameplay()
    {
        StateTransitionUtils.Transition("Gameplay");
    }

    /// <summary>
    /// Transitions to the seed selection scene for choosing plants and zombies.
    /// </summary>
    internal static void ToChooseSeeds()
    {
        StateTransitionUtils.Transition("ChooseSeeds");
    }

    /// <summary>
    /// Transitions to an loading state
    /// </summary>
    internal static void ToGameEnd()
    {
        StateTransitionUtils.Transition("Win");
    }

    /// <summary>
    /// Transitions to an loading state
    /// </summary>
    internal static void SetLoading()
    {
        Instances.GlobalPanels.GetPanel("loadingScrim")?.gameObject?.SetActive(true);
    }
}
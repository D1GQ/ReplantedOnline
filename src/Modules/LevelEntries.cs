using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using Il2CppSource.Utils;
using ReplantedOnline.Attributes;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Modules;

/// <summary>
/// Manages and provides access to level data entries.
/// This class maintains a cache of all available levels for quick lookup by name.
/// </summary>
internal static class LevelEntries
{
    private static readonly Dictionary<string, LevelEntryData> _levelNameLookup = [];

    /// <summary>
    /// Initializes the level cache by finding all LevelEntryData objects in the game resources.
    /// This should be called early in the mod initialization process.
    /// </summary>
    internal static void Initialize()
    {
        foreach (var level in Instances.DataServiceActivity.Service.AllLevelsData.EnumerateIl2CppReadonlyList())
        {
            _levelNameLookup[level.name] = level;
        }
    }

    /// <summary>
    /// Retrieves a LevelEntryData object by its name.
    /// </summary>
    /// <param name="name">The name of the level to retrieve.</param>
    /// <returns>
    /// The LevelEntryData object if found; otherwise, returns the default value for LevelEntryData.
    /// </returns>
    internal static LevelEntryData GetLevel(string name)
    {
        if (_levelNameLookup.TryGetValue(name, out var levelData))
        {
            return levelData;
        }
        return default;
    }

    /// <summary>
    /// Configures the versus arena level based on the current lobby arena type.
    /// </summary>
    internal static void SetupVersusArenaForGameplay(SelectionSet selectionSet)
    {
        var arenaType = VersusState.Arena;
        ICharacterConfig.SetArenaDefinitions(arenaType);

        var versusLevel = GetLevel("Level-Versus");
        var arena = RegisterArena.Instances.FirstOrDefault(a => a.Type == arenaType);
        if (arena is IArenaData data)
        {
            data.SetupVersusArenaForGameplay(versusLevel);
        }

        // Hide arena changing
        if (selectionSet != SelectionSet.CustomAll)
        {
            Transitions.SetFade();
            Instances.GameplayActivity.StartCoroutine(CoroutineUtils.ExecuteAfterDelay(0.5f, Transitions.FadeIn));
        }
        else
        {
            Transitions.SetFade();
            Instances.GameplayActivity.StartCoroutine(CoroutineUtils.ExecuteAfterDelay(0.2f, Transitions.FadeIn));
        }

        // Set background to new arena
        UnityEngine.Object.Destroy(Instances.GameplayActivity.BackgroundController.gameObject);
        Instances.GameplayActivity.ChangeLevelBackground(versusLevel, true);
        Instances.GameplayActivity.BackgroundController.Init(Instances.GameplayActivity.Board);
        Instances.GameplayActivity.m_boardOffset = Instances.GameplayActivity.BackgroundController.GridOffset.transform;
        Instances.GameplayActivity.m_gameplayPooler.m_gridOffset = Instances.GameplayActivity.BackgroundController.GridOffset.transform;
        Instances.GameplayActivity.Board.InitLevel();
        Instances.GameplayActivity.InitInput();
        Instances.GameplayActivity.VersusMode.m_focusCircleController = Instances.GameplayActivity.CreateFocusCircleController();
        foreach (var cursorObject in Instances.GameplayActivity.Board.CursorObjects.m_values)
        {
            cursorObject.mController = Instances.GameplayActivity.CreateCursorController(cursorObject.CursorType, cursorObject);
        }
        DataModelUtils.UpdateGameplayBoard(Instances.GameplayActivity.RootModel, Instances.GameplayActivity.Board, Instances.GameplayActivity.m_seedChooserScreen);

        // Play selecting seeds music
        if (selectionSet == SelectionSet.CustomAll)
        {
            Instances.GameplayActivity.Music.PlayMusic(MusicTune.ZenGarden, 0, 0);
        }
    }

    /// <summary>
    /// Resets the versus arena level back to its default Day configuration.
    /// </summary>
    internal static void ResetVersusArena()
    {
        var level = GetLevel("Level-Versus");
        level.m_gameArea = GameArea.Day;
        level.m_backgroundPrefab = GetLevel("Level-AdventureArea1Level1").BackgroundPrefab;
    }
}
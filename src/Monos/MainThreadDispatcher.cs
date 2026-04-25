using ReplantedOnline.Utilities;
using UnityEngine;

namespace ReplantedOnline.Monos;

/// <summary>
/// Handles execution of actions on the Unity main thread.
/// Useful for invoking Unity API calls from background threads.
/// </summary>
internal class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new();

    /// <summary>
    /// Gets the singleton instance of the MainThreadDispatcher.
    /// </summary>
    internal static MainThreadDispatcher Instance { get; private set; }

    /// <summary>
    /// Initializes the MainThreadDispatcher by creating a persistent GameObject.
    /// Should be called once during mod initialization.
    /// </summary>
    internal static void Initialize()
    {
        if (Instance != null) return;

        var go = new GameObject("MainThreadDispatcher")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<MainThreadDispatcher>();
    }

    /// <summary>
    /// Unity Update method called once per frame.
    /// Processes all queued actions on the main thread.
    /// </summary>
    private void Update()
    {
        while (_executionQueue.Count > 0)
        {
            try
            {
                _executionQueue.Dequeue().Invoke();
            }
            catch (Exception ex)
            {
                ReplantedOnlineMod.Logger.Error(typeof(MainThreadDispatcher), $"Error in main thread action: {ex}");
            }
        }
    }

    /// <summary>
    /// Queues an action to be executed on the main thread.
    /// Thread-safe method that can be called from any thread.
    /// </summary>
    /// <param name="action">The action to execute on the main thread.</param>
    internal static void Execute(Action action)
    {
        if (action == null) return;

        _executionQueue.Enqueue(action);
    }

    /// <summary>
    /// Cleans up the dispatcher when the object is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        _executionQueue.Clear();
        Instance = null;
    }
}
using ReplantedOnline.Modules;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Utilities;

/// <summary>
/// Provides utility methods for creating and managing coroutines with network awareness.
/// All coroutines will automatically stop if the local player is no longer in a lobby.
/// </summary>
internal static class CoroutineUtils
{
    /// <summary>
    /// Starts a coroutine from a managed IEnumerator, automatically wrapping it for Il2Cpp compatibility.
    /// </summary>
    /// <param name="mono">The MonoBehaviour that will start the coroutine.</param>
    /// <param name="enumerator">The managed IEnumerator to run as a coroutine.</param>
    /// <returns>A Coroutine object that can be used to control the coroutine execution.</returns>
    internal static Coroutine StartCoroutine(this MonoBehaviour mono, IEnumerator enumerator)
    {
        return mono.StartCoroutine(enumerator.WrapToIl2cpp());
    }

    /// <summary>
    /// Converts a managed IEnumerator to an Il2Cpp IEnumerator using the wrapper.
    /// </summary>
    /// <param name="enumerator">The managed enumerator to convert.</param>
    /// <returns>An Il2Cpp-compatible IEnumerator instance.</returns>
    internal static Il2CppSystem.Collections.IEnumerator WrapToIl2cpp(this IEnumerator enumerator)
    {
        return new Il2CppSystem.Collections.IEnumerator(new Il2cppEnumeratorWrapper(enumerator).Pointer);
    }

    /// <summary>
    /// Executes an action immediately as a coroutine.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>An IEnumerator that executes the action.</returns>
    internal static IEnumerator ExecuteImmediately(Action action)
    {
        action?.Invoke();
        yield break;
    }

    /// <summary>
    /// Executes an action after a specified delay.
    /// </summary>
    /// <param name="delaySeconds">Delay in seconds before executing the action.</param>
    /// <param name="action">The action to execute after the delay.</param>
    /// <returns>An IEnumerator that waits then executes the action.</returns>
    internal static IEnumerator ExecuteAfterDelay(float delaySeconds, Action action)
    {
        yield return WaitForSeconds(delaySeconds);

        action?.Invoke();
    }

    /// <summary>
    /// Waits for a specified duration, checking each frame if still in lobby.
    /// </summary>
    /// <param name="durationSeconds">Duration to wait in seconds.</param>
    /// <returns>An IEnumerator that waits for the specified duration.</returns>
    internal static IEnumerator WaitForSeconds(float durationSeconds)
    {
        float elapsedTime = 0f;

        while (elapsedTime < durationSeconds)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// Waits until a condition is met, then executes an action.
    /// </summary>
    /// <param name="condition">Function that returns true when the wait should end.</param>
    /// <param name="onComplete">Action to execute when the condition is met.</param>
    /// <returns>An IEnumerator that waits for the condition.</returns>
    internal static IEnumerator WaitForCondition(Func<bool> condition, Action onComplete)
    {
        while (!condition())
        {
            yield return null;
        }

        onComplete?.Invoke();
    }

    /// <summary>
    /// Waits until a condition is met, with a timeout.
    /// </summary>
    /// <param name="condition">Function that returns true when the wait should end.</param>
    /// <param name="timeoutSeconds">Maximum time to wait in seconds.</param>
    /// <param name="onSuccess">Action to execute if condition is met before timeout.</param>
    /// <param name="onTimeout">Action to execute if timeout occurs (optional).</param>
    /// <returns>An IEnumerator that waits for condition or timeout.</returns>
    internal static IEnumerator WaitForConditionWithTimeout(Func<bool> condition, float timeoutSeconds, Action onSuccess, Action onTimeout = null)
    {
        float elapsedTime = 0f;

        while (elapsedTime < timeoutSeconds)
        {
            if (condition())
            {
                onSuccess?.Invoke();
                yield break;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        onTimeout?.Invoke();
    }

    /// <summary>
    /// Executes a sequence of coroutines one after another.
    /// </summary>
    /// <param name="coroutines">The coroutines to execute in sequence.</param>
    /// <returns>An IEnumerator that executes all coroutines sequentially.</returns>
    internal static IEnumerator Sequence(params IEnumerator[] coroutines)
    {
        foreach (var coroutine in coroutines)
        {
            yield return coroutine;
        }
    }

    /// <summary>
    /// Executes multiple coroutines in parallel, waiting for all to complete.
    /// </summary>
    /// <param name="coroutines">The coroutines to execute in parallel.</param>
    /// <returns>An IEnumerator that executes all coroutines concurrently.</returns>
    internal static IEnumerator Parallel(params IEnumerator[] coroutines)
    {
        var enumeratorStacks = new Stack<IEnumerator>[coroutines.Length];

        // Initialize stacks for each coroutine
        for (int i = 0; i < coroutines.Length; i++)
        {
            enumeratorStacks[i] = new Stack<IEnumerator>();
            enumeratorStacks[i].Push(coroutines[i]);
        }

        int maxIterations = 100000; // Safety limit to prevent infinite loops
        int iterations = 0;

        while (iterations < maxIterations)
        {
            bool anyRunning = false;

            // Process each coroutine stack
            for (int i = 0; i < enumeratorStacks.Length; i++)
            {
                var stack = enumeratorStacks[i];

                if (stack.Count == 0)
                    continue;

                anyRunning = true;
                var currentEnumerator = stack.Peek();

                if (currentEnumerator.MoveNext())
                {
                    // If the current yield return value is another IEnumerator, push it onto the stack
                    if (currentEnumerator.Current is IEnumerator nestedEnumerator)
                    {
                        stack.Push(nestedEnumerator);
                    }
                }
                else
                {
                    // Current enumerator is done, pop it from the stack
                    stack.Pop();
                }
            }

            // Exit if no coroutines are running
            if (!anyRunning)
                break;

            iterations++;
            yield return null;
        }

        if (iterations >= maxIterations)
        {
            Debug.LogWarning($"Parallel coroutine execution exceeded maximum iterations ({maxIterations})");
        }
    }
}
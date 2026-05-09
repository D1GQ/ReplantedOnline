using UnityEngine;

namespace ReplantedOnline.Modules.Unity;

/// <summary>
/// Timer based on Time.time
/// </summary>
public sealed class UnityTimer
{
    private float _startTime = Time.time;

    /// <summary>
    /// Gets the total time elapsed since the timer was created or last reset
    /// </summary>
    /// <returns>The time in seconds since the start reference point</returns>
    public float AccumulatedTime => Time.time - _startTime;

    /// <summary>
    /// Sets the timer to trigger after a specified duration from now
    /// </summary>
    /// <param name="sDuration">The duration in seconds to wait before HasElapsed returns true</param>
    public void Set(float sDuration)
    {
        _startTime = Time.time - sDuration;
    }

    /// <summary>
    /// Checks if the specified duration has elapsed since the timer started
    /// </summary>
    /// <param name="sDuration">The duration in seconds to check against</param>
    /// <returns>True if the accumulated time is greater than or equal to the duration, otherwise false</returns>
    public bool HasElapsed(float sDuration)
    {
        return AccumulatedTime >= sDuration;
    }
}
using UnityEngine;

namespace ReplantedOnline.Modules.Unity;

/// <summary>
/// Timer based on Time.time
/// </summary>
internal sealed class UnityTimer
{
    private float _startTime = Time.time;

    /// <summary>
    /// Gets the total time elapsed since the timer was created or last reset
    /// </summary>
    /// <returns>The time in seconds since the start reference point</returns>
    public float AccumulatedTime => Time.time - _startTime;

    /// <summary>
    /// Resets the timer to start counting from the current moment
    /// </summary>
    public void Reset()
    {
        _startTime = Time.time;
    }

    /// <summary>
    /// Sets the timer by skipping forward a specified number of seconds
    /// </summary>
    /// <param name="startSeconds">The number of seconds to skip forward</param>
    public void Set(float startSeconds)
    {
        _startTime = Time.time - startSeconds;
    }

    /// <summary>
    /// Sets the timer by skipping forward a specified duration (minutes and seconds)
    /// </summary>
    /// <param name="startMinutes">The minutes to skip forward</param>
    /// <param name="startSeconds">The seconds to skip forward (additional to the minutes)</param>
    public void Set(int startMinutes, float startSeconds)
    {
        float totalSeconds = (startMinutes * 60f) + startSeconds;
        _startTime = Time.time - totalSeconds;
    }

    /// <summary>
    /// Checks if the specified duration has elapsed since the timer started
    /// </summary>
    /// <param name="seconds">The duration in seconds to check against</param>
    /// <returns>True if the accumulated time is greater than or equal to the duration, otherwise false</returns>
    public bool HasElapsed(float seconds)
    {
        return AccumulatedTime >= seconds;
    }

    /// <summary>
    /// Checks if the specified duration (minutes and seconds) has elapsed since the timer started
    /// </summary>
    /// <param name="minutes">The minutes portion of the duration</param>
    /// <param name="seconds">The seconds portion of the duration (additional to the minutes)</param>
    /// <returns>True if the accumulated time is greater than or equal to the total duration, otherwise false</returns>
    public bool HasElapsed(int minutes, float seconds)
    {
        float totalSeconds = (minutes * 60f) + seconds;
        return AccumulatedTime >= totalSeconds;
    }
}
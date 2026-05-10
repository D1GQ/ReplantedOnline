namespace ReplantedOnline.Structs;

/// <summary>
/// Represents a time value with centisecond precision (1/100 second).
/// Internal storage uses integer centiseconds for performance and accuracy.
/// </summary>
internal readonly struct IntTime
{
    private readonly int _time;

    /// <summary>
    /// Converts seconds to game time units.
    /// </summary>
    /// <param name="seconds">Time in seconds.</param>
    /// <returns>Game time units.</returns>
    internal static int From(float seconds)
    {
        return (int)(seconds * 100);
    }

    /// <summary>
    /// Converts minutes and seconds to game time units.
    /// </summary>
    /// <param name="minutes">Minutes component.</param>
    /// <param name="seconds">Seconds component.</param>
    /// <returns>Game time units.</returns>
    internal static int From(int minutes, float seconds)
    {
        return (int)((minutes * 60 + seconds) * 100);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntTime"/> from seconds.
    /// </summary>
    /// <param name="seconds">The time value in seconds. Will be converted to centiseconds (multiplied by 100).</param>
    internal IntTime(float seconds)
    {
        _time = From(seconds);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IntTime"/> from minutes and seconds.
    /// </summary>
    /// <param name="minutes">The minutes component.</param>
    /// <param name="seconds">The seconds component (including fractional seconds).</param>
    internal IntTime(int minutes, float seconds)
    {
        _time = From(minutes, seconds);
    }

    /// <summary>
    /// Creates an <see cref="IntTime"/> from a game's integer time value.
    /// </summary>
    /// <param name="gameValue">The game's time value (presumably in centiseconds).</param>
    /// <returns>A new <see cref="IntTime"/> instance representing the same time.</returns>
    internal static IntTime FromGameValue(int gameValue) => new IntTime(gameValue / 100f);

    /// <summary>
    /// Converts the time value to seconds.
    /// </summary>
    /// <returns>The time value in seconds as a floating-point number.</returns>
    internal float ToSeconds() => _time / 100f;

    /// <summary>
    /// Implicitly converts a float (seconds) to an <see cref="IntTime"/>.
    /// </summary>
    /// <param name="seconds">The time value in seconds.</param>
    public static implicit operator IntTime(float seconds) => new IntTime(seconds);

    /// <summary>
    /// Implicitly converts an <see cref="IntTime"/> to an integer centisecond value.
    /// </summary>
    /// <param name="time">The <see cref="IntTime"/> instance to convert.</param>
    public static implicit operator int(IntTime time) => time._time;

    /// <summary>
    /// Explicitly converts an <see cref="IntTime"/> to a floating-point second value.
    /// </summary>
    /// <param name="time">The <see cref="IntTime"/> instance to convert.</param>
    public static explicit operator float(IntTime time) => time.ToSeconds();
}
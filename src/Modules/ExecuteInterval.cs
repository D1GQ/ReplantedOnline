namespace ReplantedOnline.Modules;

/// <summary>
/// Provides a simple interval execution gate that alternates execution
/// </summary>
internal sealed class ExecuteInterval
{
    private uint _interval;

    /// <summary>
    /// Advances the internal counter and determines whether execution is allowed
    /// </summary>
    /// <returns>
    /// True if the current call is within the allowed execution interval; otherwise false.
    /// </returns>
    internal bool Execute()
    {
        _interval++;

        if (_interval % 2 != 0)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Resets the current interval to 0.
    /// </summary>
    internal void Reset()
    {
        _interval = 0;
    }
}
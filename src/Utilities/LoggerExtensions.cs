using MelonLoader;

namespace ReplantedOnline.Utilities;

/// <summary>
/// Provides extension methods for MelonLoader's logger instance to include class type information in log messages.
/// </summary>
internal static class LoggerExtensions
{
    /// <summary>
    /// Logs a standard informational message with the specified class type name prefixed.
    /// </summary>
    /// <param name="logger">The MelonLogger instance to log with.</param>
    /// <param name="classType">The type of the class originating the log message.</param>
    /// <param name="txt">The log message text.</param>
    internal static void Msg(this MelonLogger.Instance logger, Type classType, string txt)
    {
        logger.Msg($"[{classType.Name}] {txt}");
    }

    /// <summary>
    /// Logs a warning message with the specified class type name prefixed.
    /// </summary>
    /// <param name="logger">The MelonLogger instance to log with.</param>
    /// <param name="classType">The type of the class originating the log message.</param>
    /// <param name="txt">The log message text.</param>
    internal static void Warning(this MelonLogger.Instance logger, Type classType, string txt)
    {
        logger.Warning($"[{classType.Name}] {txt}");
    }

    /// <summary>
    /// Logs an error message with the specified class type name prefixed.
    /// </summary>
    /// <param name="logger">The MelonLogger instance to log with.</param>
    /// <param name="classType">The type of the class originating the log message.</param>
    /// <param name="txt">The log message text.</param>
    internal static void Error(this MelonLogger.Instance logger, Type classType, string txt)
    {
        logger.Error($"[{classType.Name}] {txt}");
    }

    /// <summary>
    /// Logs a prominently displayed error message with the specified class type name prefixed.
    /// </summary>
    /// <param name="logger">The MelonLogger instance to log with.</param>
    /// <param name="classType">The type of the class originating the log message.</param>
    /// <param name="txt">The log message text.</param>
    internal static void BigError(this MelonLogger.Instance logger, Type classType, string txt)
    {
        logger.BigError($"[{classType.Name}] {txt}");
    }
}
using System.Text.RegularExpressions;

namespace ReplantedOnline.Utilities.Modded;

/// <summary>
/// Provides utility methods for working with strings.
/// </summary>
internal static class StringUtils
{
    /// <summary>
    /// Removes all HTML tags and formatting from a string.
    /// </summary>
    /// <param name="text">The text to clean.</param>
    /// <returns>The plain text without HTML.</returns>
    internal static string RemoveHtmlText(string text)
    {
        text = Regex.Replace(text, "<[^>]*>", "");
        text = Regex.Replace(text, "{[^}]*}", "");
        text = text.Replace("\n", " ").Replace("\r", " ");
        text = text.Trim();

        return text;
    }

    /// <summary>
    /// Checks if a string contains HTML or special formatting.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True if the text contains HTML or formatting, false otherwise.</returns>
    internal static bool IsHtmlText(string text)
    {
        return Regex.IsMatch(text, "<[^>]*>") ||
               Regex.IsMatch(text, "{[^}]*}") ||
               text.Contains("\n") ||
               text.Contains("\r");
    }
}

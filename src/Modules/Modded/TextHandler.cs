namespace ReplantedOnline.Modules.Modded;

/// <summary>
/// Provides utility methods for handling and comparing text strings.
/// </summary>
internal static class TextHandler
{
    /// <summary>
    /// Checks if a text string matches a wildcard pattern.
    /// </summary>
    /// <param name="text">The text string to check against the wildcard pattern.</param>
    /// <param name="wildcard">The wildcard pattern to match against. Can end with '*' for prefix matching.</param>
    /// <returns><c>true</c> if the text matches the wildcard pattern (exact match or prefix match); otherwise, <c>false</c>.</returns>
    internal static bool CheckWildcardPrefix(string text, string wildcard)
    {
        if (text == wildcard)
        {
            return true;
        }

        if (wildcard.EndsWith("*") && text.StartsWith(wildcard[..^1]))
        {
            return true;
        }

        return false;
    }
}
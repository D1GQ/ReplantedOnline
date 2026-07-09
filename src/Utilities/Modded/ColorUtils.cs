using UnityEngine;

namespace ReplantedOnline.Utilities.Modded;

/// <summary>
/// Provides utility methods for working with Untiy Colors.
/// </summary>
internal static class ColorUtils
{
    /// <summary>
    /// Linearly interpolates between multiple colors based on a value within a specified range.
    /// </summary>
    /// <param name="colors">The array of colors to interpolate between.</param>
    /// <param name="lerpRange">The minimum and maximum range for the interpolation value.</param>
    /// <param name="t">The interpolation value within the specified range.</param>
    /// <param name="reverse">Whether to reverse the color array order.</param>
    /// <returns>The interpolated color.</returns>
    internal static Color LerpColor(this Color[] colors, (float min, float max) lerpRange, float t, bool reverse = false)
    {
        float normalizedT = Mathf.InverseLerp(lerpRange.min, lerpRange.max, t);

        if (colors.Length == 1)
            return colors[0];

        if (reverse)
        {
            colors.Reverse();
        }

        if (normalizedT <= 0f)
            return colors[0];
        if (normalizedT >= 1f)
            return colors[^1];

        float segmentSize = 1f / (colors.Length - 1);
        int segmentIndex = (int)(normalizedT / segmentSize);
        float segmentT = (normalizedT - segmentIndex * segmentSize) / segmentSize;

        return Color.Lerp(colors[segmentIndex], colors[segmentIndex + 1], segmentT);
    }
}

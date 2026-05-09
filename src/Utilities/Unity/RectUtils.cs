using UnityEngine;

namespace ReplantedOnline.Utilities.Unity;

/// <summary>
/// Provides utility methods for working with Unity Rect structures.
/// </summary>
internal static class RectUtils
{
    /// <summary>
    /// A non-interactable rectangle positioned far outside the visible screen area.
    /// </summary>
    internal static readonly Rect NonInteractableRect = new(99999, 99999, 0, 0);

    private const int INTERACTABLE_INDEX = 100000;

    /// <summary>
    /// Converts a noninteractable rectangle back to its interactable state by restoring its original position.
    /// </summary>
    /// <param name="rect">The rectangle to convert.</param>
    /// <returns>
    /// A new rectangle with the original position restored if it was in a non-interactable state;
    /// otherwise, returns the original rectangle unchanged.
    /// </returns>
    internal static Rect AsInteractable(this Rect rect)
    {
        if (rect.x >= INTERACTABLE_INDEX)
        {
            rect.x -= INTERACTABLE_INDEX;
        }

        if (rect.y >= INTERACTABLE_INDEX)
        {
            rect.y -= INTERACTABLE_INDEX;
        }

        return rect;
    }

    /// <summary>
    /// Converts an interactable rectangle to a noninteractable state by moving it off-screen.
    /// </summary>
    /// <param name="rect">The rectangle to convert.</param>
    /// <returns>
    /// A new rectangle positioned off-screen with its original position encoded in the coordinate offset.
    /// </returns>
    internal static Rect AsNonInteractable(this Rect rect)
    {
        if (rect.x < INTERACTABLE_INDEX)
        {
            rect.x += INTERACTABLE_INDEX;
        }

        if (rect.y < INTERACTABLE_INDEX)
        {
            rect.y += INTERACTABLE_INDEX;
        }

        return rect;
    }

    /// <summary>
    /// Determines whether the specified rectangle is in an interactable state.
    /// </summary>
    /// <param name="rect">The rectangle to check.</param>
    /// <returns>
    /// <c>true</c> if the rectangle is interactable (not moved off-screen); 
    /// otherwise, <c>false</c> if it has been moved off-screen via <see cref="AsNonInteractable"/>.
    /// </returns>
    internal static bool IsInteractable(this Rect rect)
    {
        return rect.x < INTERACTABLE_INDEX && rect.y < INTERACTABLE_INDEX;
    }
}
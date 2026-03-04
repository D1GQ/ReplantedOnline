using ReplantedOnline.Monos;
using UnityEngine;

namespace ReplantedOnline.Helper;

/// <summary>
/// Provides helper methods for rendering debug graphics in the Unity GUI system.
/// </summary>
internal static class DebugRenderHelper
{
    /// <summary>
    /// Draws a line between two points with specified thickness and color.
    /// </summary>
    /// <param name="from">The starting point of the line.</param>
    /// <param name="to">The ending point of the line.</param>
    /// <param name="thickness">The thickness of the line in pixels.</param>
    /// <param name="color">The color of the line.</param>
    internal static void Line(Vector2 from, Vector2 to, float thickness, Color color)
    {
        GUI.color = color;
        Line(from, to, thickness);
    }

    /// <summary>
    /// Draws a line between two points with specified thickness.
    /// </summary>
    /// <param name="from">The starting point of the line.</param>
    /// <param name="to">The ending point of the line.</param>
    /// <param name="thickness">The thickness of the line in pixels.</param>
    internal static void Line(Vector2 from, Vector2 to, float thickness)
    {
        var delta = (to - from).normalized;
        var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        GUIUtility.RotateAroundPivot(angle, from);
        Box(from, Vector2.right * (from - to).magnitude, thickness, false);
        GUIUtility.RotateAroundPivot(-angle, from);
    }

    /// <summary>
    /// Draws a rectangle with specified position, size, thickness, and color.
    /// </summary>
    /// <param name="position">The position of the rectangle.</param>
    /// <param name="size">The size of the rectangle.</param>
    /// <param name="thickness">The thickness of the rectangle borders in pixels.</param>
    /// <param name="color">The color of the rectangle borders.</param>
    /// <param name="centered">If true, position represents the center of the rectangle; otherwise, it represents the top-left corner.</param>
    internal static void Box(Vector2 position, Vector2 size, float thickness, Color color, bool centered = true)
    {
        GUI.color = color;
        Box(position, size, thickness, centered);
    }

    /// <summary>
    /// Draws a rectangle with specified position, size, and thickness.
    /// </summary>
    /// <param name="position">The position of the rectangle.</param>
    /// <param name="size">The size of the rectangle.</param>
    /// <param name="thickness">The thickness of the rectangle borders in pixels.</param>
    /// <param name="centered">If true, position represents the center of the rectangle; otherwise, it represents the top-left corner.</param>
    internal static void Box(Vector2 position, Vector2 size, float thickness, bool centered = true)
    {
        var upperLeft = centered ? position - size / 2f : position;

        var whiteStyle = new GUIStyle();
        whiteStyle.normal.background = Texture2D.whiteTexture;

        GUI.Label(new Rect(upperLeft.x, upperLeft.y, size.x, thickness), GUIContent.none, whiteStyle);
        GUI.Label(new Rect(upperLeft.x, upperLeft.y, thickness, size.y), GUIContent.none, whiteStyle);
        GUI.Label(new Rect(upperLeft.x + size.x, upperLeft.y, thickness, size.y), GUIContent.none, whiteStyle);
        GUI.Label(new Rect(upperLeft.x, upperLeft.y + size.y, size.x + thickness, thickness), GUIContent.none, whiteStyle);
    }

    /// <summary>
    /// Draws a small dot at the specified position with the given color.
    /// </summary>
    /// <param name="position">The position where to draw the dot.</param>
    /// <param name="color">The color of the dot.</param>
    internal static void Dot(Vector2 position, Color color)
    {
        GUI.color = color;
        Dot(position);
    }

    /// <summary>
    /// Draws a small dot at the specified position.
    /// </summary>
    /// <param name="position">The position where to draw the dot.</param>
    internal static void Dot(Vector2 position)
    {
        Box(position - Vector2.one, Vector2.one * 2f, 1f);
    }

    /// <summary>
    /// Draws text with a drop shadow effect, always centered in the specified position.
    /// </summary>
    /// <param name="X">The X coordinate for the center position of the text.</param>
    /// <param name="Y">The Y coordinate for the center position of the text.</param>
    /// <param name="W">The width available for the text.</param>
    /// <param name="H">The height available for the text.</param>
    /// <param name="strs">The text strings to display.</param>
    /// <param name="col">The color of the main text (shadow will be black).</param>
    /// <param name="offset">The offsets between each text (will be scaled automatically if not provided).</param>
    internal static void Strings(float X, float Y, float W, float H, string[] strs, Color col, Vector2? offset = null)
    {
        Vector2 actualOffset = offset ?? new Vector2(0f, 15f);

        float currentX = X;
        float currentY = Y;

        foreach (var str in strs)
        {
            String(currentX, currentY, W, H, str, col);
            currentX += actualOffset.x;
            currentY += actualOffset.y;
        }
    }

    /// <summary>
    /// Draws text with a drop shadow effect, always centered in the specified position.
    /// </summary>
    /// <param name="X">The X coordinate for the center position of the text.</param>
    /// <param name="Y">The Y coordinate for the center position of the text.</param>
    /// <param name="W">The width available for the text.</param>
    /// <param name="H">The height available for the text.</param>
    /// <param name="str">The text string to display.</param>
    /// <param name="col">The color of the main text (shadow will be black).</param>
    internal static void String(float X, float Y, float W, float H, string str, Color col)
    {
        GUIContent content = new(str);
        Vector2 size = InfoDisplay.Style.CalcSize(content);

        float fX = X - size.x / 2f;
        float fY = Y - size.y / 2f;

        InfoDisplay.Style.normal.textColor = Color.black;
        GUI.Label(new Rect(fX, fY, size.x, H), str, InfoDisplay.Style);

        InfoDisplay.Style.normal.textColor = col;
        GUI.Label(new Rect(fX + 1f, fY + 1f, size.x, H), str, InfoDisplay.Style);
    }

    /// <summary>
    /// Draws a circle outline with specified center, radius, thickness, and color.
    /// </summary>
    /// <param name="center">The center point of the circle.</param>
    /// <param name="radius">The radius of the circle.</param>
    /// <param name="thickness">The thickness of the circle outline in pixels.</param>
    /// <param name="color">The color of the circle outline.</param>
    internal static void Circle(Vector2 center, float radius, float thickness, Color color)
    {
        GUI.color = color;
        Vector2 previousPoint = center + new Vector2(radius, 0);

        for (int i = 1; i <= 360; i++)
        {
            float angle = i * Mathf.Deg2Rad;
            Vector2 nextPoint = center + new Vector2(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle));
            Line(previousPoint, nextPoint, thickness);
            previousPoint = nextPoint;
        }
    }
}
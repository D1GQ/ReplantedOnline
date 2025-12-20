using ReplantedOnline.Network.Online;
using ReplantedOnline.Patches.Client;
using UnityEngine;

namespace ReplantedOnline.Monos;

/// <summary>
/// Displays mod information on the screen.
/// </summary>
internal sealed class InfoDisplay : MonoBehaviour
{
    /// <summary>
    /// Initializes the InfoDisplay component and creates a persistent GameObject.
    /// </summary>
    internal static void Initialize()
    {
        var go = new GameObject(nameof(InfoDisplay));
        go.AddComponent<InfoDisplay>();
        DontDestroyOnLoad(go);
    }

    private GUIStyle _style;

    /// <summary>
    /// Called every frame for GUI rendering.
    /// </summary>
    /// <remarks>
    /// Draws the mod information label in the bottom-right corner of the screen.
    /// Adjusts transparency based on whether the player is in a lobby.
    /// </remarks>
    public void OnGUI()
    {
        if (_style == null)
        {
            _style = new GUIStyle()
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                padding = new RectOffset() { left = 4, right = 4, top = 2, bottom = 2 }
            };
            GUIStyle.Internal_Copy(_style, GUI.skin.label);
        }

        float alpha = NetLobby.AmInLobby() ? 1f : 0.8f;
        _style.normal.textColor = new Color(1f, 1f, 1f, alpha);
        GUI.backgroundColor = new Color(23f / 255f, 23f / 255f, 23f / 255f, 1f);
        GUI.contentColor = Color.white;
        GUI.color = Color.white;

        var info = GetInfo();
        Vector2 labelSize = _style.CalcSize(new GUIContent(info));
        float padding = 5f;

        GUI.Label(
            new Rect(
                Screen.width - labelSize.x - padding,
                Screen.height - labelSize.y - padding,
                labelSize.x,
                labelSize.y
            ),
            info,
            _style
        );
    }

    /// <summary>
    /// Gets the formatted information string to display.
    /// </summary>
    private static string GetInfo()
    {
        return $"{ModInfo.MOD_NAME}: v{ModInfo.MOD_VERSION_FORMATTED}-{ModInfo.RELEASE_DATE} Server: {Enum.GetName(SteamPatch.AppServer).ToLower()}";
    }
}
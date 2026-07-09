#pragma warning disable CS0162

using Il2CppSteamworks;
using MelonLoader;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.MonoScripts.Unity;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Network.Reloaded.Client.Routing;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ReplantedOnline.MonoScripts.Modded;

/// <summary>
/// Displays mod information on the screen.
/// </summary>
[RegisterTypeInIl2Cpp]
internal sealed class InfoDisplay : MonoBehaviour
{
    internal static bool DebugEnabled { get; private set; }
    internal static bool DebugLoggerEnabled { get; private set; }

    private bool _enabled = true;
    internal static GUIStyle? Style;

    private void Update()
    {
        if (ReplantedOnlineMod.ModInfo.DEBUG)
        {
            if (Keyboard.current.f1Key.wasPressedThisFrame)
            {
                _enabled = !_enabled;
            }
        }

        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            DebugEnabled = !DebugEnabled;
        }

        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            DebugLoggerEnabled = !DebugLoggerEnabled;
        }
    }

    private void OnGUI()
    {
        if (Style == null)
        {
            Style = new GUIStyle()
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                padding = new RectOffset() { left = 4, right = 4, top = 2, bottom = 2 }
            };
            GUIStyle.Internal_Copy(Style, GUI.skin.label);
        }

        float padding = 5f;

        // Bottom right info
        if (_enabled)
        {
            string info = GetInfo();

            DrawLabelWithOutline(
                info,
                new Rect(
                    Screen.width - Style.CalcSize(new GUIContent(info)).x - padding,
                    Screen.height - Style.CalcSize(new GUIContent(info)).y - padding,
                    Style.CalcSize(new GUIContent(info)).x,
                    Style.CalcSize(new GUIContent(info)).y
                ),
                Style,
                Color.white,
                Color.black
            );

            if (ReloadedLobby.AmInLobby() && NetworkManager.NetworkHeartbeat != null)
            {
                string ping = $"Ping: {NetworkManager.NetworkHeartbeat.GetEstimatedPing()}ms";
                DrawLabelWithOutline(
                    ping,
                    new Rect(
                        padding,
                        Screen.height - Style.CalcSize(new GUIContent(ping)).y - padding,
                        Style.CalcSize(new GUIContent(ping)).x,
                        Style.CalcSize(new GUIContent(ping)).y
                    ),
                    Style,
                    Color.white,
                    Color.black
                );
            }
        }

        // Top left debug info
        if (DebugEnabled)
        {
            var debugInfo = GetDebugInfo();
            DrawLabelWithOutline(
                debugInfo,
                new Rect(padding, padding,
                        Style.CalcSize(new GUIContent(debugInfo)).x,
                        Style.CalcSize(new GUIContent(debugInfo)).y),
                Style,
                Color.white * 0.95f,
                Color.black
            );
        }
    }

    /// <summary>
    /// Draws a text label with an outline effect using multiple offset labels.
    /// </summary>
    /// <param name="text">The text to display.</param>
    /// <param name="rect">The position and size of the text area.</param>
    /// <param name="style">The GUIStyle to use for the text.</param>
    /// <param name="textColor">The color of the main text.</param>
    /// <param name="outlineColor">The color of the outline.</param>
    /// <param name="outlineWidth">The width/thickness of the outline in pixels. Default is 1.</param>
    private static void DrawLabelWithOutline(string text, Rect rect, GUIStyle style, Color textColor, Color outlineColor, int outlineWidth = 1)
    {
        style.normal.textColor = outlineColor;
        GUI.Label(new Rect(rect.x - outlineWidth, rect.y, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x + outlineWidth, rect.y, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x, rect.y - outlineWidth, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x, rect.y + outlineWidth, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x - outlineWidth, rect.y - outlineWidth, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x + outlineWidth, rect.y - outlineWidth, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x - outlineWidth, rect.y + outlineWidth, rect.width, rect.height), text, style);
        GUI.Label(new Rect(rect.x + outlineWidth, rect.y + outlineWidth, rect.width, rect.height), text, style);
        style.normal.textColor = textColor;
        GUI.Label(rect, text, style);
    }

    /// <summary>
    /// Gets the formatted information string to display.
    /// </summary>
    private static string GetInfo()
    {
        return $"{ReplantedOnlineMod.ModInfo.MOD_NAME}: v{ReplantedOnlineMod.ModInfo.MOD_VERSION_FORMATTED}-{ReplantedOnlineMod.ModInfo.RELEASE_DATE}";
    }

    /// <summary>
    /// NEW: Gets debug information to display in top left corner.
    /// </summary>
    private static string GetDebugInfo()
    {
        StringBuilder sb = new();

        sb.AppendLine("Debug Info >");

        if (DebugLoggerEnabled)
        {
            sb.AppendLine($" Debug Logger: True");
        }

        sb.AppendLine($" Mod Signature: {ReplantedOnlineMod.ModInfo.ModSignature.SignatureHash}");

        sb.AppendLine($" Steam initialized: {SteamClient.initialized}");

        sb.AppendLine($" Steam Appid: {SteamClient.AppId}");

        sb.AppendLine($" Prefabs: {RuntimePrefab.Prefabs.Count}");

        if (ReloadedLobby.LobbyData != null)
        {
            sb.AppendLine("Lobby Info >");

            if (!ReloadedLobby.LobbyData.ReadyForNetworkObjects)
            {
                sb.AppendLine($" Not Ready For Network Objects!");
            }
            else
            {
                sb.AppendLine($" Network Objects: {ReloadedLobby.LobbyData.NetworkObjectsSpawned.Count}");
            }

            if (!ReloadedLobby.LobbyData.HasStarted.Value)
            {
                sb.AppendLine(" Versus Phase: Lobby");
            }
            else
            {
                sb.AppendLine($" Versus Phase: {Enum.GetName(Instances.GameplayActivity.VersusMode.Phase)}");
            }

            sb.AppendLine($" Clients: {ReloadedLobby.LobbyData.AllClients.Count}");

            foreach (var client in ReloadedLobby.LobbyData.AllClients.Values)
            {
                sb.AppendLine($"{client.Name} Client Info >");

                sb.AppendLine($" ID: {client.ClientId}");

                sb.AppendLine($" Team: {Enum.GetName(client.Team)}");

                sb.AppendLine($" AmLocal: {client.AmLocal}");

                sb.AppendLine($" AmHost: {client.AmHost}");

                sb.AppendLine($" Ready: {client.Ready.Value}");
            }
        }

        return sb.ToString();
    }
}
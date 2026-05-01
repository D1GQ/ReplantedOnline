using Il2CppInterop.Runtime.Attributes;
using ReplantedOnline.Network.Client.Object;
using ReplantedOnline.Network.Client.Object.Replanted;
using ReplantedOnline.Utilities;
using UnityEngine;

namespace ReplantedOnline.Monos;

internal sealed class NetworkedDebugger : MonoBehaviour
{
    private NetworkObject _instance;
    private GUIStyle _boxOutlineStyle;
    private GUIStyle _boxFillStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _infoStyle;
    private GUIStyle _backgroundStyle;
    private Texture2D _whiteTexture;
    private bool _isMouseOver;
    private Vector3 _cachedScreenPos;

    [HideFromIl2Cpp]
    internal void Initialize(NetworkObject networkObj)
    {
        _instance = networkObj;
        CreateWhiteTexture();
        CreateStyles();
    }

    [HideFromIl2Cpp]
    private void CreateWhiteTexture()
    {
        _whiteTexture = new Texture2D(1, 1);
        _whiteTexture.SetPixel(0, 0, Color.white);
        _whiteTexture.Apply();
    }

    [HideFromIl2Cpp]
    private void CreateStyles()
    {
        Texture2D outlineTex = new(1, 1);
        outlineTex.SetPixel(0, 0, new Color(1f, 1f, 0f, 0.8f));
        outlineTex.Apply();

        Texture2D fillTex = new(1, 1);
        fillTex.SetPixel(0, 0, new Color(1f, 1f, 0f, 0.15f));
        fillTex.Apply();

        Texture2D bgTex = new(1, 1);
        bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.7f));
        bgTex.Apply();

        _boxOutlineStyle = new GUIStyle();
        _boxOutlineStyle.normal.background = outlineTex;

        _boxFillStyle = new GUIStyle();
        _boxFillStyle.normal.background = fillTex;

        _backgroundStyle = new GUIStyle();
        _backgroundStyle.normal.background = bgTex;

        _labelStyle = new GUIStyle
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _labelStyle.normal.textColor = Color.yellow;

        _infoStyle = new GUIStyle
        {
            fontSize = 10,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };
        _infoStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        if (!InfoDisplay.DebugEnabled) return;
        if (_instance == null) return;
        if (!_instance.IsOnNetwork) return;

        if (_instance is ZombieNetworked zombieNetworked)
        {
            DebugZombie(zombieNetworked);
        }
        else if (_instance is PlantNetworked plantNetworked)
        {
            DebugPlant(plantNetworked);
        }
    }

    [HideFromIl2Cpp]
    private void DebugZombie(ZombieNetworked zombieNetworked)
    {
        var zombie = zombieNetworked._Zombie;

        bool isDead = zombie == null || zombie.mDead;

        if (!isDead)
        {
            Vector3 worldPos = zombie.mController.transform.position + zombie.mController.GetRenderOffset();
            Rect hitboxRect = zombie.mZombieRect;
            _cachedScreenPos = WorldToScreen(worldPos + new Vector3(0f, 100f, 0f));

            string phaseInfo = $"{zombie.mZombiePhase}: {zombie.mPhaseCounter}";
            DrawDebugInfo(worldPos + new Vector3(0f, 100f, 0f), hitboxRect, zombieNetworked.gameObject.name, phaseInfo);

            Vector3 screenPos = WorldToScreen(worldPos + new Vector3(0f, 100f, 0f));
            DrawSyncPosition(zombieNetworked, worldPos, screenPos);
        }
        else
        {
            if (_cachedScreenPos != Vector3.zero)
            {
                DrawDeadObjectDebug(_cachedScreenPos, zombieNetworked.gameObject.name);
            }
        }
    }

    [HideFromIl2Cpp]
    private void DebugPlant(PlantNetworked plantNetworked)
    {
        var plant = plantNetworked._Plant;

        bool isDead = plant == null || plant.mDead;

        if (!isDead)
        {
            Vector3 worldPos = plant.mController.transform.position + plant.mController.GetRenderOffset();
            Rect hitboxRect = plant.mPlantRect;
            _cachedScreenPos = WorldToScreen(worldPos + new Vector3(0f, 100f, 0f));

            string stateInfo = $"{plant.mState}: {plant.mStateCountdown}";
            DrawDebugInfo(worldPos + new Vector3(0f, 100f, 0f), hitboxRect, plantNetworked.gameObject.name, stateInfo);
        }
        else
        {

            if (_cachedScreenPos != Vector3.zero)
            {
                DrawDeadObjectDebug(_cachedScreenPos, plantNetworked.gameObject.name);
            }
        }
    }

    [HideFromIl2Cpp]
    private void DrawDebugInfo(Vector3 worldPos, Rect hitboxRect, string objectName, string info)
    {
        Vector3 screenPos = WorldToScreen(worldPos);
        if (screenPos.z < 0) return;

        Vector2 hitboxScreenSize = WorldSizeToScreen(hitboxRect.width, hitboxRect.height, worldPos);

        Rect boxRect = new(
            screenPos.x - (hitboxScreenSize.x / 2f),
            screenPos.y - (hitboxScreenSize.y / 2f),
            hitboxScreenSize.x,
            hitboxScreenSize.y
        );

        _isMouseOver = boxRect.Contains(Event.current.mousePosition);

        Color outlineColor = _isMouseOver ? new Color(0f, 1f, 1f, 0.8f) : new Color(1f, 1f, 0f, 0.8f);
        Color fillColor = _isMouseOver ? new Color(0f, 1f, 1f, 0.15f) : new Color(1f, 1f, 0f, 0.15f);

        UpdateBoxColors(outlineColor, fillColor);

        GUI.Box(boxRect, "", _boxFillStyle);
        DrawBoxOutline(boxRect, 2f);

        Vector2 nameSize = _labelStyle.CalcSize(new GUIContent(objectName));
        Rect nameRect = new(
            screenPos.x - (nameSize.x / 2f) - 4f,
            boxRect.y + boxRect.height + 5f,
            nameSize.x + 8f,
            nameSize.y + 4f
        );
        GUI.Box(nameRect, "", _backgroundStyle);

        Color originalTextColor = _labelStyle.normal.textColor;
        GUI.Label(new Rect(nameRect.x + 4f, nameRect.y + 2f, nameSize.x, nameSize.y), objectName, _labelStyle);
        _labelStyle.normal.textColor = originalTextColor;

        if (_isMouseOver && !string.IsNullOrEmpty(info))
        {
            Vector2 infoSize = _infoStyle.CalcSize(new GUIContent(info));
            Rect infoRect = new Rect(
                screenPos.x - (infoSize.x / 2f) - 4f,
                nameRect.y + nameRect.height + 5f,
                infoSize.x + 8f,
                infoSize.y + 4f
            );
            GUI.Box(infoRect, "", _backgroundStyle);
            GUI.Label(new Rect(infoRect.x + 4f, infoRect.y + 2f, infoSize.x, infoSize.y), info, _infoStyle);
        }
    }

    [HideFromIl2Cpp]
    private void DrawDeadObjectDebug(Vector3 cachedScreenPos, string objectName)
    {

        Vector3 screenPos = cachedScreenPos;
        if (screenPos.z < 0) return;

        Vector2 boxSize = new(100f, 100f);
        Vector2 hitboxScreenSize = WorldSizeToScreen(boxSize.x, boxSize.y, Vector3.zero);

        Rect boxRect = new Rect(
            screenPos.x - (hitboxScreenSize.x / 2f),
            screenPos.y - (hitboxScreenSize.y / 2f),
            hitboxScreenSize.x,
            hitboxScreenSize.y
        );

        _isMouseOver = boxRect.Contains(Event.current.mousePosition);

        UpdateBoxColors(new Color(1f, 0f, 0f, 0.8f), new Color(1f, 0f, 0f, 0.15f));

        GUI.Box(boxRect, "", _boxFillStyle);
        DrawBoxOutline(boxRect, 2f);

        Color originalTextColor = _labelStyle.normal.textColor;
        _labelStyle.normal.textColor = Color.red;

        Vector2 nameSize = _labelStyle.CalcSize(new GUIContent(objectName));
        Rect nameRect = new(
            screenPos.x - (nameSize.x / 2f) - 4f,
            boxRect.y + boxRect.height + 5f,
            nameSize.x + 8f,
            nameSize.y + 4f
        );
        GUI.Box(nameRect, "", _backgroundStyle);
        GUI.Label(new Rect(nameRect.x + 4f, nameRect.y + 2f, nameSize.x, nameSize.y), objectName, _labelStyle);
        _labelStyle.normal.textColor = originalTextColor;
    }

    [HideFromIl2Cpp]
    private void DrawSyncPosition(ZombieNetworked zombieNetworked, Vector3 currentWorldPos, Vector3 currentScreenPos)
    {

        if (zombieNetworked.LogicComponent.LastSyncPosX.HasValue)
        {

            Vector3 syncWorldPos = new Vector3(
                PvZRUtils.GetGridOffsetXPosFromBoardXPos(zombieNetworked.LogicComponent.LastSyncPosX.Value),
                currentWorldPos.y,
                currentWorldPos.z
            );

            Vector3 syncScreenPos = WorldToScreen(syncWorldPos + new Vector3(0f, 100f, 0f));

            if (syncScreenPos.z >= 0)
            {

                DrawLine(currentScreenPos, syncScreenPos, 2f, Color.magenta);

                Vector2 syncBoxSize = new Vector2(50f, 50f);
                Vector2 syncScreenSize = WorldSizeToScreen(syncBoxSize.x, syncBoxSize.y, syncWorldPos);

                Rect syncBoxRect = new(
                    syncScreenPos.x - (syncScreenSize.x / 2f),
                    syncScreenPos.y - (syncScreenSize.y / 2f),
                    syncScreenSize.x,
                    syncScreenSize.y
                );

                Color originalOutline = _boxOutlineStyle.normal.background.GetPixel(0, 0);
                Color originalFill = _boxFillStyle.normal.background.GetPixel(0, 0);

                UpdateBoxColors(Color.magenta, new Color(1f, 0f, 1f, 0.15f));
                GUI.Box(syncBoxRect, "", _boxFillStyle);
                DrawBoxOutline(syncBoxRect, 2f);

                UpdateBoxColors(originalOutline, originalFill);
            }
        }
    }

    private void DrawLine(Vector3 from, Vector3 to, float thickness, Color color)
    {
        Vector2 from2D = new Vector2(from.x, from.y);
        Vector2 to2D = new Vector2(to.x, to.y);
        Vector2 delta = to2D - from2D;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        float distance = delta.magnitude;

        Texture2D lineTex = new(1, 1);
        lineTex.SetPixel(0, 0, color);
        lineTex.Apply();

        GUIStyle lineStyle = new();
        lineStyle.normal.background = lineTex;

        GUIUtility.RotateAroundPivot(angle, from2D);
        GUI.Box(new Rect(from2D.x, from2D.y, distance, thickness), GUIContent.none, lineStyle);
        GUIUtility.RotateAroundPivot(-angle, from2D);
    }

    private void UpdateBoxColors(Color outlineColor, Color fillColor)
    {
        Texture2D outlineTex = new Texture2D(1, 1);
        outlineTex.SetPixel(0, 0, outlineColor);
        outlineTex.Apply();
        _boxOutlineStyle.normal.background = outlineTex;

        Texture2D fillTex = new Texture2D(1, 1);
        fillTex.SetPixel(0, 0, fillColor);
        fillTex.Apply();
        _boxFillStyle.normal.background = fillTex;
    }

    private void DrawBoxOutline(Rect rect, float thickness)
    {
        GUI.Box(new Rect(rect.x, rect.y, rect.width, thickness), "", _boxOutlineStyle);
        GUI.Box(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), "", _boxOutlineStyle);
        GUI.Box(new Rect(rect.x, rect.y, thickness, rect.height), "", _boxOutlineStyle);
        GUI.Box(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), "", _boxOutlineStyle);
    }

    private static Vector3 WorldToScreen(Vector3 worldPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;

        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
        screenPos.y = Screen.height - screenPos.y;
        return screenPos;
    }

    private static Vector2 WorldSizeToScreen(float worldWidth, float worldHeight, Vector3 worldPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return new Vector2(50f, 50f);

        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        Vector3 worldCorner = worldPos + new Vector3(worldWidth, worldHeight, 0);
        Vector3 screenCorner = cam.WorldToScreenPoint(worldCorner);

        screenPos.y = Screen.height - screenPos.y;
        screenCorner.y = Screen.height - screenCorner.y;

        float screenWidth = Mathf.Abs(screenCorner.x - screenPos.x);
        float screenHeight = Mathf.Abs(screenCorner.y - screenPos.y);

        return new Vector2(
            Mathf.Max(20f, screenWidth),
            Mathf.Max(20f, screenHeight)
        );
    }
}
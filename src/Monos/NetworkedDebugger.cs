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
    private GUIStyle _lineStyle;
    private Texture2D _whiteTexture;
    private bool _isMouseOver;
    private Vector3 _cachedScreenPos;

    private Texture2D _lineTexture;
    private Color _currentLineColor;

    private Color _currentOutlineColor;
    private Color _currentFillColor;

    private Rect _tempRect = new();


    private Vector3 _cachedWorldPos;
    private Rect _cachedHitboxRect;

    [HideFromIl2Cpp]
    internal void Initialize(NetworkObject networkObj)
    {
        _instance = networkObj;
        CreateWhiteTexture();
        CreateStyles();
        CreateLineTexture();
    }

    [HideFromIl2Cpp]
    private void CreateWhiteTexture()
    {
        _whiteTexture = new Texture2D(1, 1);
        _whiteTexture.SetPixel(0, 0, Color.white);
        _whiteTexture.Apply();
    }

    [HideFromIl2Cpp]
    private void CreateLineTexture()
    {
        _lineTexture = new Texture2D(1, 1);
        _lineTexture.SetPixel(0, 0, Color.white);
        _lineTexture.Apply();
        _lineStyle = new GUIStyle();
        _lineStyle.normal.background = _lineTexture;
    }

    [HideFromIl2Cpp]
    private void CreateStyles()
    {
        // Create textures once and reuse them
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
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.yellow }
        };

        _infoStyle = new GUIStyle
        {
            fontSize = 10,
            alignment = TextAnchor.UpperLeft,
            wordWrap = false,
            normal = { textColor = Color.white }
        };

        _currentOutlineColor = new Color(1f, 1f, 0f, 0.8f);
        _currentFillColor = new Color(1f, 1f, 0f, 0.15f);
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

        if (zombie != null && !zombie.HasNetworked())
        {
            Vector3 worldPos = zombie.mController.transform.position + zombie.mController.GetRenderOffset();
            _cachedScreenPos = WorldToScreen(worldPos + new Vector3(0f, 100f, 0f));
            DrawErrorObjectDebug(_cachedScreenPos, zombieNetworked.gameObject.name, Color.black);
        }
        else if (!isDead)
        {
            Vector3 worldPos = zombie.mController.transform.position + zombie.mController.GetRenderOffset();
            Rect hitboxRect = zombie.mZombieRect;
            _cachedScreenPos = WorldToScreen(worldPos + new Vector3(0f, 100f, 0f));

            string phaseInfo = $"{zombie.mZombiePhase}: {zombie.mPhaseCounter}";
            DrawDebugInfo(worldPos + new Vector3(0f, 100f, 0f), hitboxRect, zombieNetworked.gameObject.name, phaseInfo);

            if (zombieNetworked.LogicComponent.LastSyncPosX.HasValue)
            {
                DrawSyncPosition(zombieNetworked, worldPos);
            }
        }
        else
        {
            if (_cachedScreenPos != Vector3.zero)
            {
                DrawErrorObjectDebug(_cachedScreenPos, zombieNetworked.gameObject.name, Color.red);
            }
        }
    }

    [HideFromIl2Cpp]
    private void DebugPlant(PlantNetworked plantNetworked)
    {
        var plant = plantNetworked._Plant;

        bool isDead = plant == null || plant.mDead;

        if (plant != null && !plant.HasNetworked())
        {
            Vector3 worldPos = plant.mController.transform.position + plant.mController.GetRenderOffset();
            _cachedScreenPos = WorldToScreen(worldPos + new Vector3(0f, 100f, 0f));
            DrawErrorObjectDebug(_cachedScreenPos, plantNetworked.gameObject.name, Color.black);
        }
        else if (!isDead)
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
                DrawErrorObjectDebug(_cachedScreenPos, plantNetworked.gameObject.name, Color.red);
            }
        }
    }

    [HideFromIl2Cpp]
    private void DrawDebugInfo(Vector3 worldPos, Rect hitboxRect, string objectName, string info)
    {
        Vector3 screenPos = WorldToScreen(worldPos);
        if (screenPos.z < 0) return;

        Vector2 hitboxScreenSize = WorldSizeToScreen(hitboxRect.width, hitboxRect.height, worldPos);

        _tempRect.Set(
            screenPos.x - (hitboxScreenSize.x / 2f),
            screenPos.y - (hitboxScreenSize.y / 2f),
            hitboxScreenSize.x,
            hitboxScreenSize.y
        );

        _isMouseOver = _tempRect.Contains(Event.current.mousePosition);

        Color outlineColor = _isMouseOver ? new Color(0f, 1f, 1f, 0.8f) : new Color(1f, 1f, 0f, 0.8f);
        Color fillColor = _isMouseOver ? new Color(0f, 1f, 1f, 0.15f) : new Color(1f, 1f, 0f, 0.15f);

        if (outlineColor != _currentOutlineColor || fillColor != _currentFillColor)
        {
            UpdateBoxColors(outlineColor, fillColor);
        }

        GUI.Box(_tempRect, "", _boxFillStyle);
        DrawBoxOutline(_tempRect, 2f);

        Vector2 nameSize = _labelStyle.CalcSize(new GUIContent(objectName));
        Rect nameRect = new(
            screenPos.x - (nameSize.x / 2f) - 4f,
            _tempRect.y + _tempRect.height + 5f,
            nameSize.x + 8f,
            nameSize.y + 4f
        );
        GUI.Box(nameRect, "", _backgroundStyle);
        GUI.Label(new Rect(nameRect.x + 4f, nameRect.y + 2f, nameSize.x, nameSize.y), objectName, _labelStyle);

        if (_isMouseOver && !string.IsNullOrEmpty(info))
        {
            GUIContent infoContent = new GUIContent(info);
            Vector2 infoSize = _infoStyle.CalcSize(infoContent);

            float padding = 8f;
            Rect infoRect = new Rect(
                screenPos.x - (infoSize.x / 2f) - (padding / 2f),
                nameRect.y + nameRect.height + 5f,
                infoSize.x + padding,
                infoSize.y + padding
            );

            if (infoRect.x < 0) infoRect.x = 0;
            if (infoRect.x + infoRect.width > Screen.width)
                infoRect.x = Screen.width - infoRect.width;
            if (infoRect.y + infoRect.height > Screen.height)
                infoRect.y = Screen.height - infoRect.height;

            GUI.Box(infoRect, "", _backgroundStyle);

            Rect textRect = new Rect(
                infoRect.x + (padding / 2f),
                infoRect.y + (padding / 2f),
                infoSize.x,
                infoSize.y
            );
            GUI.Label(textRect, info, _infoStyle);
        }
    }

    [HideFromIl2Cpp]
    private void DrawErrorObjectDebug(Vector3 cachedScreenPos, string objectName, Color color)
    {
        Vector3 screenPos = cachedScreenPos;
        if (screenPos.z < 0) return;

        Vector2 boxSize = new(100f, 100f);
        Vector2 hitboxScreenSize = WorldSizeToScreen(boxSize.x, boxSize.y, Vector3.zero);

        _tempRect.Set(
            screenPos.x - (hitboxScreenSize.x / 2f),
            screenPos.y - (hitboxScreenSize.y / 2f),
            hitboxScreenSize.x,
            hitboxScreenSize.y
        );

        _isMouseOver = _tempRect.Contains(Event.current.mousePosition);

        Color outlineColor = new Color(color.r, color.g, color.b, 0.8f);
        Color fillColor = new Color(color.r, color.g, color.b, 0.15f);

        if (outlineColor != _currentOutlineColor || fillColor != _currentFillColor)
        {
            UpdateBoxColors(outlineColor, fillColor);
        }

        GUI.Box(_tempRect, "", _boxFillStyle);
        DrawBoxOutline(_tempRect, 2f);

        Color originalTextColor = _labelStyle.normal.textColor;
        _labelStyle.normal.textColor = color;

        Vector2 nameSize = _labelStyle.CalcSize(new GUIContent(objectName));
        Rect nameRect = new(
            screenPos.x - (nameSize.x / 2f) - 4f,
            _tempRect.y + _tempRect.height + 5f,
            nameSize.x + 8f,
            nameSize.y + 4f
        );
        GUI.Box(nameRect, "", _backgroundStyle);
        GUI.Label(new Rect(nameRect.x + 4f, nameRect.y + 2f, nameSize.x, nameSize.y), objectName, _labelStyle);
        _labelStyle.normal.textColor = originalTextColor;
    }

    [HideFromIl2Cpp]
    private void DrawSyncPosition(ZombieNetworked zombieNetworked, Vector3 currentWorldPos)
    {
        Vector3 currentScreenPos = WorldToScreen(currentWorldPos + new Vector3(0f, 100f, 0f));
        if (currentScreenPos.z < 0) return;

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

            UpdateBoxColors(Color.magenta, new Color(1f, 0f, 1f, 0.15f));
            GUI.Box(syncBoxRect, "", _boxFillStyle);
            DrawBoxOutline(syncBoxRect, 2f);

            UpdateBoxColors(_currentOutlineColor, _currentFillColor);
        }
    }

    private void DrawLine(Vector3 from, Vector3 to, float thickness, Color color)
    {
        Vector2 from2D = new Vector2(from.x, from.y);
        Vector2 to2D = new Vector2(to.x, to.y);
        Vector2 delta = to2D - from2D;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        float distance = delta.magnitude;

        // Update line texture color only if changed
        if (color != _currentLineColor)
        {
            _lineTexture.SetPixel(0, 0, color);
            _lineTexture.Apply();
            _currentLineColor = color;
        }

        GUIUtility.RotateAroundPivot(angle, from2D);
        GUI.Box(new Rect(from2D.x, from2D.y, distance, thickness), GUIContent.none, _lineStyle);
        GUIUtility.RotateAroundPivot(-angle, from2D);
    }

    private void UpdateBoxColors(Color outlineColor, Color fillColor)
    {
        var outlineTex = _boxOutlineStyle.normal.background;
        outlineTex.SetPixel(0, 0, outlineColor);
        outlineTex.Apply();

        var fillTex = _boxFillStyle.normal.background;
        fillTex.SetPixel(0, 0, fillColor);
        fillTex.Apply();

        _currentOutlineColor = outlineColor;
        _currentFillColor = fillColor;
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

    private void OnDestroy()
    {
        if (_whiteTexture != null) Destroy(_whiteTexture);
        if (_lineTexture != null) Destroy(_lineTexture);
        if (_boxOutlineStyle?.normal.background != null) Destroy(_boxOutlineStyle.normal.background);
        if (_boxFillStyle?.normal.background != null) Destroy(_boxFillStyle.normal.background);
        if (_backgroundStyle?.normal.background != null) Destroy(_backgroundStyle.normal.background);
    }
}
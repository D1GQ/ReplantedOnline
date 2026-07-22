using Il2CppReloaded.Gameplay;
using Il2CppTMPro;
using MelonLoader;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Utilities.Il2Cpp;
using ReplantedOnline.Utilities.Modded;
using ReplantedOnline.Utilities.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace ReplantedOnline.MonoScripts.Modded;

/// <summary>
/// Manages the timer display panel for versus mode gameplay.
/// </summary>
[RegisterTypeInIl2Cpp]
internal sealed class TimerPanel : MonoBehaviour
{
    private readonly Color[] TimerColors = [Color.green, Color.yellow, Color.red];
    private bool _init;
    private TextMeshProUGUI _text = null!;
    private RectTransform _shovelContainer = null!;

    /// <summary>
    /// Initializes the timer panel UI elements and sets up the visual hierarchy.
    /// </summary>
    /// <param name="MainHUDLayout">The parent RectTransform that the timer panel will be attached to.</param>
    /// <param name="textPrefab">The TextMeshProUGUI prefab used to create the timer text display.</param>
    /// <param name="shovelContainer">The RectTransform reference for the shovel container that gets repositioned during gameplay.</param>
    internal static void Initialize(RectTransform MainHUDLayout, TextMeshProUGUI textPrefab, RectTransform shovelContainer)
    {
        GameObject timerPanelObj = new("TimerPanel");
        var timerPanelRect = timerPanelObj.AddComponent<RectTransform>();
        timerPanelRect.SetParent(MainHUDLayout);
        timerPanelRect.SetSiblingIndex(6);
        timerPanelRect.anchoredPosition3D = new(0f, 968f, 0f);
        TimerPanel timerPanel = timerPanelObj.AddComponent<TimerPanel>();

        GameObject imageObj = new("Image");
        var imageRect = imageObj.AddComponent<RectTransform>();
        imageRect.SetParent(timerPanelRect);
        imageRect.anchoredPosition = Vector3.zero;
        imageRect.localScale = new(4.5f, 1.5f, 1f);
        var image = imageObj.AddComponent<Image>();
        image.sprite = ReplantedOnlineMod.Assets.Sprites.TimerPanel;

        timerPanel._text = Instantiate(textPrefab);
        timerPanel._text.name = "Text";
        timerPanel._text.SetText(string.Empty);
        timerPanel._text.transform.SetParent(timerPanelRect);
        timerPanel._text.transform.localScale = new(1.8f, 1.8f, 1.8f);
        timerPanel._text.gameObject.DestroyAllTextLocalizers();
        timerPanel._text.gameObject.SetActive(true);
        if (timerPanel._text.transform.Il2CppTryCast<RectTransform>(out var textRect))
        {
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(200, 100);
        }

        timerPanel._shovelContainer = shovelContainer;
        timerPanel._init = true;
    }

    private void Update()
    {
        if (!_init)
            return;

        if (_text == null || _shovelContainer == null)
            return;

        if (Instances.GameplayActivity?.VersusMode == null)
            return;

        _shovelContainer.anchoredPosition = new Vector2(128f, -370f);

        float currentTime = VersusState.VersusTimeSynced;
        float suddenDeathStartTime = VersusMode.k_suddenDeathStartTime;

        if (currentTime < 0f)
        {
            _text.SetText("05:00");
            _text.color = TimerColors.FirstOrDefault();
            return;
        }

        float timeToDisplay;
        float colorAlpha = 1f;
        if (currentTime < suddenDeathStartTime)
        {
            timeToDisplay = suddenDeathStartTime - currentTime;
        }
        else
        {
            timeToDisplay = currentTime + 1f - suddenDeathStartTime;
            colorAlpha = Mathf.PingPong(Time.time * 2f, 0.8f) + 0.2f;
        }

        timeToDisplay = Mathf.Min(timeToDisplay, 3599f);

        _text.SetText(FormatTime(timeToDisplay));
        _text.color = TimerColors.LerpColor((0f, suddenDeathStartTime), currentTime);
        _text.color = new(_text.color.r, _text.color.g, _text.color.b, colorAlpha);
    }

    /// <summary>
    /// Formats a time value in seconds into a MM:SS string format.
    /// </summary>
    /// <param name="timeInSeconds">The time in seconds to format. Values less than 0 will be clamped to 0, and values greater than 3599 seconds will be clamped to 3599 seconds (59 minutes and 59 seconds).</param>
    /// <returns>A string formatted as "MM:SS" representing the time.</returns>
    private static string FormatTime(float timeInSeconds)
    {
        if (timeInSeconds < 0) timeInSeconds = 0;

        timeInSeconds = Mathf.Min(timeInSeconds, 3599f);

        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);

        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
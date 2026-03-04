using Il2CppReloaded.Services;
using MelonLoader;
using ReplantedOnline.Modules;
using ReplantedOnline.Utilities;
using System.Collections;
using System.Reflection;
using UnityEngine;
using static ReplantedOnline.Managers.BloomEngineManager;

namespace ReplantedOnline.Managers;

/// <summary>
/// Manages audio functionality for the Replanted Online mod, including loading and replacing game audio.
/// </summary>
internal static class AudioManager
{
    /// <summary>
    /// Initializes the audio manager and sets up audio replacements.
    /// </summary>
    internal static void Initialize()
    {
        MelonCoroutines.Start(WaitForAppCore(() =>
        {
            CatchAudio();
            OnModifyMusic(BloomConfigs.ModifyMusic.Value, false);
        }));
    }

    private static AudioClip MainMenuTheme;
    private static AudioClip CustomMainMenuTheme;

    /// <summary>
    /// Captures and caches audio.
    /// </summary>
    private static void CatchAudio()
    {
        MainMenuTheme = GetAudio(MusicFile.MainMusic, MusicTune.TitleCrazyDaveMainTheme);
        MainMenuTheme.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
        CustomMainMenuTheme = Assembly.GetExecutingAssembly().LoadWavFromResources("ReplantedOnline.Resources.Sounds.CrazyDaveMainTheme-Compressed.wav");
        CustomMainMenuTheme.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
    }

    /// <summary>
    /// Switches between the original or custom music.
    /// </summary>
    internal static void OnModifyMusic(bool custom, bool fromSetting)
    {
        if (MainMenuTheme == null || CustomMainMenuTheme == null)
        {
            return;
        }

        if (Instances.AppCore?.m_audioSourcesService?.m_musicSourceMappings == null)
        {
            return;
        }

        if (custom)
        {
            ReplaceAudio(MusicFile.MainMusic, MusicTune.TitleCrazyDaveMainTheme, CustomMainMenuTheme, fromSetting);

        }
        else
        {
            ReplaceAudio(MusicFile.MainMusic, MusicTune.TitleCrazyDaveMainTheme, MainMenuTheme, fromSetting);
        }
    }

    /// <summary>
    /// Replaces a specific game audio track with a custom WAV file from embedded resources.
    /// </summary>
    /// <param name="id">The MusicFile identifier for the audio to replace.</param>
    /// <param name="tune">The MusicTune identifier for the specific audio track.</param>
    /// <param name="clip">The clip to replace with.</param>
    /// <param name="replay">If the audio should replay.</param>
    internal static void ReplaceAudio(MusicFile id, MusicTune tune, AudioClip clip, bool replay)
    {
        var Audio = Instances.AppCore.m_audioSourcesService.m_musicSourceMappings.FirstOrDefault(am => am.IsId(id, tune));
        Audio?.m_audioSource?.Stop();
        Audio?.m_audioSource?.m_audioSource?.clip = clip;
        if (replay)
        {
            Audio?.m_audioSource?.Play(true, true);
        }
    }

    /// <summary>
    /// Retrieves the AudioClip currently assigned to a specific game audio track.
    /// </summary>
    /// <param name="id">The MusicFile identifier for the audio to retrieve.</param>
    /// <param name="tune">The MusicTune identifier for the specific audio track.</param>
    /// <returns>
    /// The AudioClip currently assigned to the specified audio track, or null if no matching
    /// audio source is found.
    /// </returns>
    internal static AudioClip GetAudio(MusicFile id, MusicTune tune)
    {
        var Audio = Instances.AppCore.m_audioSourcesService.m_musicSourceMappings.FirstOrDefault(am => am.IsId(id, tune));
        return Audio?.m_audioSource.m_audioSource.clip;
    }

    /// <summary>
    /// Coroutine that waits for the AppCore instance to be available before executing a callback.
    /// </summary>
    /// <param name="callback">The action to execute once AppCore is available.</param>
    /// <returns>An IEnumerator for the coroutine.</returns>
    private static IEnumerator WaitForAppCore(Action callback)
    {
        while (Instances.AppCore == null)
        {
            yield return null;
        }

        callback();
    }
}
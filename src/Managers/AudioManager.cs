using Il2CppReloaded.Services;
using MelonLoader;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using System.Collections;
using System.Reflection;

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
            ReplaceAudio(MusicFile.MainMusic, MusicTune.TitleCrazyDaveMainTheme, "ReplantedOnline.Resources.Sounds.CrazyDaveMainTheme.wav");
        }));
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

    /// <summary>
    /// Replaces a specific game audio track with a custom WAV file from embedded resources.
    /// </summary>
    /// <param name="id">The MusicFile identifier for the audio to replace.</param>
    /// <param name="tune">The MusicTune identifier for the specific audio track.</param>
    /// <param name="resourceClipPath">The fully qualified path to the embedded WAV resource.</param>
    internal static void ReplaceAudio(MusicFile id, MusicTune tune, string resourceClipPath)
    {
        var Audio = Instances.AppCore.m_audioSourcesService.m_musicSourceMappings.FirstOrDefault(am => am.IsId(id, tune));
        if (Audio != null)
        {
            var clip = Assembly.GetExecutingAssembly().LoadWavFromResources(resourceClipPath);
            if (clip != null)
            {
                Audio.m_audioSource.m_audioSource.clip = clip;
            }
        }
    }
}
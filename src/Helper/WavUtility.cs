using MelonLoader;
using System.Reflection;
using UnityEngine;

namespace ReplantedOnline.Helper;

/// <summary>
/// Provides utility methods for loading and converting WAV audio files.
/// </summary>
internal static class WavUtility
{
    /// <summary>
    /// Loads a WAV audio clip from an embedded resource in the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly containing the embedded WAV resource.</param>
    /// <param name="resourcePath">The fully qualified name of the embedded resource.</param>
    /// <returns>An AudioClip containing the loaded WAV data, or null if loading fails.</returns>
    internal static AudioClip LoadWavFromResources(this Assembly assembly, string resourcePath)
    {
        try
        {
            using Stream stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
            {
                MelonLogger.Error($"Resource not found: {resourcePath}");
                return null;
            }

            using MemoryStream ms = new();
            stream.CopyTo(ms);
            byte[] wavBytes = ms.ToArray();
            return ToAudioClip(wavBytes);
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"Failed to load WAV from resources: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Loads a WAV audio clip from a file on disk.
    /// </summary>
    /// <param name="filePath">The full path to the WAV file.</param>
    /// <returns>An AudioClip containing the loaded WAV data, or null if loading fails.</returns>
    internal static AudioClip LoadWavFromDisk(string filePath)
    {
        if (!File.Exists(filePath))
        {
            MelonLogger.Error($"File not found: {filePath}");
            return null;
        }

        if (Path.GetExtension(filePath).ToLower() != ".wav")
        {
            MelonLogger.Error("Only .wav files are supported.");
            return null;
        }

        try
        {
            byte[] wavBytes = File.ReadAllBytes(filePath);
            return ToAudioClip(wavBytes);
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"Failed to load WAV: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Converts raw WAV byte data into an AudioClip.
    /// </summary>
    /// <param name="wavBytes">The raw WAV file bytes.</param>
    /// <returns>An AudioClip containing the converted WAV data.</returns>
    internal static AudioClip ToAudioClip(byte[] wavBytes)
    {
        int sampleRate = BitConverter.ToInt32(wavBytes, 24);
        int channels = BitConverter.ToInt16(wavBytes, 22);
        int samples = BitConverter.ToInt32(wavBytes, 40) / 2;

        int offset = 36;
        while (offset < wavBytes.Length - 8)
        {
            if (wavBytes[offset] == 'd' && wavBytes[offset + 1] == 'a' &&
                wavBytes[offset + 2] == 't' && wavBytes[offset + 3] == 'a')
            {
                offset += 4;
                offset += 4;
                break;
            }
            offset++;
        }

        float[] floatData = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            short sample = BitConverter.ToInt16(wavBytes, offset + i * 2);
            floatData[i] = sample / 32768f;
        }

        AudioClip clip = AudioClip.Create("LoadedWav", samples / channels, channels, sampleRate, false);
        clip.SetData(floatData, 0);

        return clip;
    }
}
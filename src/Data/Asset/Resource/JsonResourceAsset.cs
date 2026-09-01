using System.Text.Json;

namespace ReplantedOnline.Data.Asset.Resource;

/// <summary>
/// Represents a JSON configuration asset loaded from embedded resources.
/// </summary>
/// <typeparam name="T">The type of the configuration object to deserialize from JSON.</typeparam>
/// <param name="path">The resource path to the JSON file.</param>
internal sealed class JsonResourceAsset<T>(string path) : ResourceAsset<T>(path) where T : class
{
    /// <summary>
    /// The resource path to the JSON configuration file within the mod's embedded resources.
    /// </summary>
    private readonly string _path = path;

    /// <summary>
    /// Loads the JSON configuration asset from the mod's embedded resources.
    /// </summary>
    internal override void Load()
    {
        try
        {
            using var stream = ReplantedOnlineMod.ModInfo.Assembly.GetManifestResourceStream(_path);
            if (stream != null)
            {
                using StreamReader reader = new(stream);
                string content = reader.ReadToEnd();
                T? config = JsonSerializer.Deserialize<T>(content);
                if (config != null)
                {
                    Loadded = true;
                    Failed = false;
                    Asset = config;
                }
                else
                {
                    Loadded = false;
                    Failed = true;
                }
            }
        }
        catch
        {
            Loadded = false;
            Failed = true;
        }
    }
}
using ReplantedOnline.Data.Json.Config.Reloaded;

namespace ReplantedOnline.Data;

/// <summary>
/// Manages data for ReplantedOnline.
/// </summary>
internal static class DataManager
{
    /// <summary>
    /// Gets the versus mode configuration containing all seed packet and zombie data.
    /// </summary>
    internal static VersusModeConfig VersusModeConfig { get; private set; } = null!;

    /// <summary>
    /// Initializes the data manager.
    /// </summary>
    internal static void Initialize()
    {
        var stream = ReplantedOnlineMod.ModInfo.Assembly.GetManifestResourceStream("ReplantedOnline.Resources.VersusModeConfig.json");
        if (stream != null)
        {
            using var streamReader = new StreamReader(stream);
            string json = streamReader.ReadToEnd();

            var config = VersusModeConfig.DeserializeObject(json);
            VersusModeConfig = config!;
        }
        else
        {
            throw new InvalidOperationException("Could not find embedded resource: ReplantedOnline.Resources.VersusModeConfig.json");
        }
    }
}
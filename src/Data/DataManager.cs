using ReplantedOnline.Data.Asset.Resource;
using ReplantedOnline.Data.Json.Config.Reloaded;

namespace ReplantedOnline.Data;

/// <summary>
/// Manages data for ReplantedOnline.
/// </summary>
internal static class DataManager
{
    /// <summary>
    /// The JSON resource asset for VersusModeConfig.
    /// </summary>
    private static readonly JsonResourceAsset<VersusModeConfig> VersusModeConfigAsset = new("ReplantedOnline.Resources.VersusModeConfig.json");

    /// <summary>
    /// Gets the versus mode configuration containing all seed packet and zombie data.
    /// </summary>
    internal static VersusModeConfig VersusModeConfig { get; set; } = null!;

    /// <summary>
    /// Initializes the data manager.
    /// </summary>
    internal static void Initialize()
    {
        VersusModeConfigAsset.Load();
        if (VersusModeConfigAsset.Loadded)
        {
            VersusModeConfig = VersusModeConfigAsset.Asset;
        }
        else
        {
            throw new InvalidOperationException("Could not load VersusModeConfigAsset.");
        }
    }
}
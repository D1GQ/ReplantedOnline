using Il2CppReloaded.Gameplay;
using ReplantedOnline.Data.Json.Converters;
using ReplantedOnline.Structs;
using System.Text.Json.Serialization;

namespace ReplantedOnline.Data.Json.Config.Reloaded;

/// <summary>
/// Configuration data for a specific seed packet in the versus mode.
/// </summary>
internal sealed class SeedPacketConfig : JsonObject<SeedPacketConfig>
{
    /// <summary>
    /// Gets the type of seed this configuration applies to.
    /// </summary>
    [JsonConverter(typeof(JsonSeedTypeConverter))]
    public SeedType Type { get; init; }

    /// <summary>
    /// Gets the sun cost required to use this seed packet.
    /// </summary>
    public int Cost { get; init; }

    /// <summary>
    /// Gets the sun cost surplus for nocturnal plants during the night.
    /// </summary>
    public int NocturnalCostSurplus { get; init; } = 0;

    /// <summary>
    /// Gets the base cooldown time in seconds before this seed packet can be used again.
    /// </summary>
    [JsonConverter(typeof(IntTimeConverter))]
    public IntTime RefreshTime { get; init; }

    /// <summary>
    /// Gets the cooldown time in seconds during Sudden Death mode.
    /// </summary>
    [JsonConverter(typeof(IntTimeConverter))]
    public IntTime SuddenDeathRefresh { get; init; }
}
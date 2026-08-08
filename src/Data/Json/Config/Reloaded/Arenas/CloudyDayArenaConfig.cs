using Il2CppReloaded.Gameplay;
using ReplantedOnline.Data.Json.Converters;
using System.Text.Json.Serialization;

namespace ReplantedOnline.Data.Json.Config.Reloaded.Arenas;

/// <summary>
/// Configuration for the Cloudy Day arena.
/// </summary>
internal sealed class CloudyDayArenaConfig : JsonObject<CloudyDayArenaConfig>
{
    /// <summary>
    /// Gets the list of seed types that are disabled during rainy phases of the arena.
    /// </summary>
    [JsonConverter(typeof(JsonSeedTypeListConverter))]
    public List<SeedType> DisabledSeedPacketsInRain { get; init; } = [];

    /// <summary>
    /// Gets the duration of the sunny phase.
    /// </summary>
    public float SunnyPhaseTime { get; init; }

    /// <summary>
    /// Gets the duration of the rainy phase.
    /// </summary>
    public float RainPhaseTime { get; init; }

    /// <summary>
    /// Gets the cost reduction multiplier (0-1 range, e.g., 0.5 = 50% reduction).
    /// </summary>
    public float CostReductionMultiplier { get; init; } = 0.5f;

    /// <summary>
    /// Gets the rounding step for the reduced cost (e.g., 5 rounds to nearest multiple of 5).
    /// Set to 0 to disable rounding.
    /// </summary>
    public int CostReductionRoundStep { get; init; } = 5;

    /// <summary>
    /// Gets the maximum refresh time after reduction.
    /// </summary>
    public int RefreshTimeMaxValue { get; init; } = 100;

    /// <summary>
    /// Gets the power exponent for refresh time reduction (applied before multiplication).
    /// </summary>
    public float RefreshTimePower { get; init; } = 0.7f;

    /// <summary>
    /// Gets the multiplier for refresh time after applying the power.
    /// </summary>
    public float RefreshTimeMultiplier { get; init; } = 1.5f;
}
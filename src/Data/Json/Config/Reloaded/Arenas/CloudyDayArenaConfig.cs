using Il2CppReloaded.Gameplay;
using ReplantedOnline.Data.Json.Converters;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Utilities.Modded;
using System.Text.Json.Serialization;

namespace ReplantedOnline.Data.Json.Config.Reloaded.Arenas;

/// <summary>
/// Configuration for the Cloudy Day arena.
/// </summary>
internal sealed class CloudyDayArenaConfig : JsonObject, INetworkConfigSerializable
{
    /// <summary>
    /// Gets the list of seed types that are disabled during rainy phases of the arena.
    /// </summary>
    [JsonConverter(typeof(JsonSeedTypeListConverter))]
    public List<SeedType> DisabledSeedPacketsInRain { get; set; } = [];

    /// <summary>
    /// Gets the duration of the sunny phase.
    /// </summary>
    public float SunnyPhaseTime { get; set; }

    /// <summary>
    /// Gets the duration of the rainy phase.
    /// </summary>
    public float RainPhaseTime { get; set; }

    /// <summary>
    /// Gets the cost reduction multiplier (0-1 range, e.g., 0.5 = 50% reduction).
    /// </summary>
    public float CostReductionMultiplier { get; set; }

    /// <summary>
    /// Gets the rounding step for the reduced cost (e.g., 5 rounds to nearest multiple of 5).
    /// Set to 0 to disable rounding.
    /// </summary>
    public int CostReductionRoundStep { get; set; }

    /// <summary>
    /// Gets the maximum refresh time after reduction.
    /// </summary>
    public int RefreshTimeMaxValue { get; set; }

    /// <summary>
    /// Gets the power exponent for refresh time reduction (applied before multiplication).
    /// </summary>
    public float RefreshTimePower { get; set; }

    /// <summary>
    /// Gets the multiplier for refresh time after applying the power.
    /// </summary>
    public float RefreshTimeMultiplier { get; set; }

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter)
    {
        packetWriter.WritePackedInt(DisabledSeedPacketsInRain.Count);
        foreach (var seedType in DisabledSeedPacketsInRain)
        {
            packetWriter.WriteEnum(seedType);
        }
        packetWriter.WritePackedFloat(SunnyPhaseTime);
        packetWriter.WritePackedFloat(RainPhaseTime);
        packetWriter.WritePackedFloat(CostReductionMultiplier);
        packetWriter.WritePackedInt(CostReductionRoundStep);
        packetWriter.WritePackedInt(RefreshTimeMaxValue);
        packetWriter.WritePackedFloat(RefreshTimePower);
        packetWriter.WritePackedFloat(RefreshTimeMultiplier);
    }

    /// <inheritdoc/>
    public void Deserialize(PacketReader packetReader)
    {
        int count = packetReader.ReadPackedInt();
        DisabledSeedPacketsInRain.Clear();
        for (int i = 0; i < count; i++)
        {
            var seedType = packetReader.ReadEnum<SeedType>();
            DisabledSeedPacketsInRain.Add(seedType);
        }
        SunnyPhaseTime = packetReader.ReadPackedFloat();
        RainPhaseTime = packetReader.ReadPackedFloat();
        CostReductionMultiplier = packetReader.ReadPackedFloat();
        CostReductionRoundStep = packetReader.ReadPackedInt();
        RefreshTimeMaxValue = packetReader.ReadPackedInt();
        RefreshTimePower = packetReader.ReadPackedFloat();
        RefreshTimeMultiplier = packetReader.ReadPackedFloat();
    }
}
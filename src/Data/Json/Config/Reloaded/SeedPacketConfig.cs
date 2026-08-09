using Il2CppReloaded.Gameplay;
using ReplantedOnline.Data.Json.Converters;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Structs;
using System.Text.Json.Serialization;

namespace ReplantedOnline.Data.Json.Config.Reloaded;

/// <summary>
/// Configuration data for a specific seed packet in the versus mode.
/// </summary>
internal sealed class SeedPacketConfig : JsonObject<SeedPacketConfig>, INetworkConfigSerializable
{
    /// <summary>
    /// Gets the type of seed this configuration applies to.
    /// </summary>
    [JsonConverter(typeof(JsonSeedTypeConverter))]
    public SeedType Type { get; set; }

    /// <summary>
    /// Gets the sun cost required to use this seed packet.
    /// </summary>
    public int Cost { get; set; }

    /// <summary>
    /// Gets the sun cost surplus for nocturnal plants during the night.
    /// </summary>
    public int NocturnalCostSurplus { get; set; } = 0;

    /// <summary>
    /// Gets the base cooldown time in seconds before this seed packet can be used again.
    /// </summary>
    [JsonConverter(typeof(IntTimeConverter))]
    public IntTime RefreshTime { get; set; }

    /// <summary>
    /// Gets the cooldown time in seconds during Sudden Death mode.
    /// </summary>
    [JsonConverter(typeof(IntTimeConverter))]
    public IntTime SuddenDeathRefresh { get; set; }

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter)
    {
        packetWriter.WriteEnum(Type);
        packetWriter.WriteInt(Cost);
        packetWriter.WriteInt(NocturnalCostSurplus);
        packetWriter.WriteInt(RefreshTime);
        packetWriter.WriteInt(SuddenDeathRefresh);
    }

    /// <inheritdoc/>
    public void Deserialize(PacketReader packetReader)
    {
        Type = packetReader.ReadEnum<SeedType>();
        Cost = packetReader.ReadInt();
        NocturnalCostSurplus = packetReader.ReadInt();
        RefreshTime = IntTime.FromGameValue(packetReader.ReadInt());
        SuddenDeathRefresh = IntTime.FromGameValue(packetReader.ReadInt());
    }
}
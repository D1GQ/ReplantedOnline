using Il2CppReloaded.Gameplay;
using ReplantedOnline.Data.Json.Converters;
using ReplantedOnline.Network.Reloaded.Serialization;
using System.Text.Json.Serialization;

namespace ReplantedOnline.Data.Json.Config.Reloaded;

/// <summary>
/// Configuration data for a specific plant type in the versus mode.
/// </summary>
internal sealed class PlantConfig : SeedPacketConfig
{
    /// <summary>
    /// Gets the type of seed this configuration applies to.
    /// </summary>
    [JsonConverter(typeof(JsonSeedTypeConverter))]
    public SeedType Type { get; set; }

    /// <summary>
    /// Gets the base health of the plant.
    /// </summary>
    public int Health { get; set; }

    /// <inheritdoc/>
    public override void Serialize(PacketWriter packetWriter)
    {
        packetWriter.WriteEnum(Type);
        packetWriter.WriteInt(Health);
        base.Serialize(packetWriter);
    }

    /// <inheritdoc/>
    public override void Deserialize(PacketReader packetReader)
    {
        Type = packetReader.ReadEnum<SeedType>();
        Health = packetReader.ReadInt();
        base.Deserialize(packetReader);
    }
}

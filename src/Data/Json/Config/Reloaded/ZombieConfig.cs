using Il2CppReloaded.Gameplay;
using ReplantedOnline.Network.Reloaded.Serialization;
using System.Text.Json.Serialization;

namespace ReplantedOnline.Data.Json.Config.Reloaded;

/// <summary>
/// Configuration data for a specific zombie type in the versus mode.
/// </summary>
internal sealed class ZombieConfig : SeedPacketConfig
{
    /// <summary>
    /// Gets the type of zombie this configuration applies to.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ZombieType Type { get; set; }

    /// <summary>
    /// Gets the base health of the zombie's body.
    /// </summary>
    public int BodyHealth { get; set; }

    /// <summary>
    /// Gets the health of the zombie's armor (if any).
    /// </summary>
    public int ArmorHealth { get; set; }

    /// <inheritdoc/>
    public override void Serialize(PacketWriter packetWriter)
    {
        packetWriter.WriteEnum(Type);
        packetWriter.WriteInt(BodyHealth);
        packetWriter.WriteInt(ArmorHealth);
        base.Serialize(packetWriter);
    }

    /// <inheritdoc/>
    public override void Deserialize(PacketReader packetReader)
    {
        Type = packetReader.ReadEnum<ZombieType>();
        BodyHealth = packetReader.ReadInt();
        ArmorHealth = packetReader.ReadInt();
        base.Deserialize(packetReader);
    }
}
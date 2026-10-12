using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Network.Reloaded.Server;

/// <summary>
/// Represents member data for a connected player in an IP server lobby.
/// Stores player identification and custom key-value data.
/// </summary>
internal sealed class ReloadedMemberData
{
    /// <summary>
    /// Gets or sets the display name of the player.
    /// </summary>
    internal string PlayerName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier for this member.
    /// </summary>
    internal ID MemberId { get; set; }

    /// <summary>
    /// Custom key-value data associated with this member.
    /// </summary>
    internal Dictionary<string, string> Data = [];

    /// <summary>
    /// Serializes the member data to a packet writer.
    /// </summary>
    internal void Serialize(PacketWriter packetWriter, bool init)
    {
        packetWriter.WriteString(PlayerName);
        packetWriter.WriteID(MemberId);
        if (init)
        {
            SerializeData(packetWriter);
        }
    }

    /// <summary>
    /// Deserializes member data from a packet reader.
    /// </summary>
    internal void Deserialize(PacketReader packetReader, bool init)
    {
        PlayerName = packetReader.ReadString();
        MemberId = packetReader.ReadID();
        if (init)
        {
            DeserializeData(packetReader);
        }
    }

    /// <summary>
    /// Serializes the custom data dictionary to a packet writer.
    /// </summary>
    internal void SerializeData(PacketWriter packetWriter)
    {
        packetWriter.WritePackedInt(Data.Count);
        foreach (var data in Data)
        {
            packetWriter.WriteString(data.Key);
            packetWriter.WriteString(data.Value);
        }
    }

    /// <summary>
    /// Deserializes the custom data dictionary from a packet reader.
    /// </summary>
    internal void DeserializeData(PacketReader packetReader)
    {
        Dictionary<string, string> data = [];
        int dataCount = packetReader.ReadPackedInt();
        for (int i = 0; i < dataCount; i++)
        {
            string key = packetReader.ReadString();
            string value = packetReader.ReadString();
            data[key] = value;
        }
        Data = data;
    }
}
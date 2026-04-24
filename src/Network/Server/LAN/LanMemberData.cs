using ReplantedOnline.Network.Packet;
using ReplantedOnline.Structs;

namespace ReplantedOnline.Network.Server.LAN;

/// <summary>
/// Represents member data for a connected player in a LAN server lobby.
/// Stores player identification and custom key-value data.
/// </summary>
internal sealed class LanMemberData
{
    /// <summary>
    /// Gets or sets the display name of the player.
    /// </summary>
    internal string PlayerName { get; set; }

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
    /// <param name="packetWriter">The packet writer to write to.</param>
    /// <param name="init">If true, also serializes the custom data dictionary.</param>
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
    /// <param name="packetReader">The packet reader to read from.</param>
    /// <param name="init">If true, also deserializes the custom data dictionary.</param>
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
    /// <param name="packetWriter">The packet writer to write to.</param>
    internal void SerializeData(PacketWriter packetWriter)
    {
        packetWriter.WriteInt(Data.Count);
        foreach (var data in Data)
        {
            packetWriter.WriteString(data.Key);
            packetWriter.WriteString(data.Value);
        }
    }

    /// <summary>
    /// Deserializes the custom data dictionary from a packet reader.
    /// </summary>
    /// <param name="packetReader">The packet reader to read from.</param>
    internal void DeserializeData(PacketReader packetReader)
    {
        Dictionary<string, string> data = [];
        int dataCount = packetReader.ReadInt();
        for (int i = 0; i < dataCount; i++)
        {
            string key = packetReader.ReadString();
            string value = packetReader.ReadString();
            data[key] = value;
        }
        Data = data;
    }
}
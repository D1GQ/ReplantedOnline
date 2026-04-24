using ReplantedOnline.Enums.Network;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Structs;

namespace ReplantedOnline.Network.Server.LAN;

/// <summary>
/// Provides serialization and deserialization methods for LAN server protocol packets.
/// </summary>
internal static class LanServerProtocol
{
    /// <summary>
    /// Serializes a lobby data update request.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write to.</param>
    /// <param name="key">The data key to set or remove.</param>
    /// <param name="value">The data value (ignored if removing).</param>
    /// <param name="remove">If true, removes the key instead of setting it.</param>
    internal static void SerializeSetLobbyData(PacketWriter packetWriter, string key, string value, bool remove)
    {
        packetWriter.WriteBool(remove);
        packetWriter.WriteString(key);
        if (!remove)
        {
            packetWriter.WriteString(value);
        }
    }

    /// <summary>
    /// Deserializes a lobby data update request.
    /// </summary>
    /// <param name="packetReader">The packet reader to read from.</param>
    /// <returns>A tuple containing the key, value, and remove flag.</returns>
    internal static (string key, string value, bool remove) DeserializeSetLobbyData(PacketReader packetReader)
    {
        var remove = packetReader.ReadBool();
        var key = packetReader.ReadString();
        var value = string.Empty;
        if (!remove)
        {
            value = packetReader.ReadString();
        }
        return (key, value, remove);
    }

    /// <summary>
    /// Serializes a handshake request packet.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write to.</param>
    /// <param name="playerName">The name of the player requesting connection.</param>
    /// <param name="memberId">The ID of the member requesting connection.</param>
    internal static void SerializeHandshakeRequest(PacketWriter packetWriter, string playerName, ID memberId)
    {
        packetWriter.WriteString(playerName);
        packetWriter.WriteID(memberId);
    }

    /// <summary>
    /// Deserializes a handshake request packet.
    /// </summary>
    /// <param name="packetReader">The packet reader to read from.</param>
    /// <returns>A tuple containing the player name and member ID.</returns>
    internal static (string playerName, ID memberId) DeserializeHandshakeRequest(PacketReader packetReader)
    {
        var playerName = packetReader.ReadString();
        var memberId = packetReader.ReadID();
        return (playerName, memberId);
    }

    /// <summary>
    /// Serializes a handshake acceptance packet.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write to.</param>
    /// <param name="lobbyId">The ID of the lobby the member is joining.</param>
    internal static void SerializeHandshakeAccept(PacketWriter packetWriter, ID lobbyId)
    {
        packetWriter.WriteID(lobbyId);
    }

    /// <summary>
    /// Deserializes a handshake acceptance packet.
    /// </summary>
    /// <param name="packetReader">The packet reader to read from.</param>
    /// <returns>The lobby ID.</returns>
    internal static ID DeserializeHandshakeAccept(PacketReader packetReader)
    {
        return packetReader.ReadID();
    }

    /// <summary>
    /// Serializes a handshake rejection packet.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write to.</param>
    /// <param name="reason">The reason for rejection.</param>
    internal static void SerializeHandshakeReject(PacketWriter packetWriter, string reason)
    {
        packetWriter.WriteString(reason);
    }

    /// <summary>
    /// Deserializes a handshake rejection packet.
    /// </summary>
    /// <param name="packetReader">The packet reader to read from.</param>
    /// <returns>The rejection reason.</returns>
    internal static string DeserializeHandshakeReject(PacketReader packetReader)
    {
        return packetReader.ReadString();
    }

    /// <summary>
    /// Serializes a member synchronization packet.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write to.</param>
    /// <param name="members">Dictionary of all connected members.</param>
    internal static void SerializeSyncMembers(PacketWriter packetWriter, Dictionary<ID, LanMemberData> members)
    {
        packetWriter.WriteInt(members.Count);
        foreach (var member in members.Values)
        {
            member.Serialize(packetWriter, true);
        }
    }

    /// <summary>
    /// Deserializes a member synchronization packet.
    /// </summary>
    /// <param name="packetReader">The packet reader to read from.</param>
    /// <returns>A dictionary of all synchronized members.</returns>
    internal static Dictionary<ID, LanMemberData> DeserializeSyncMembers(PacketReader packetReader)
    {
        int memberCount = packetReader.ReadInt();
        var members = new Dictionary<ID, LanMemberData>();

        for (int i = 0; i < memberCount; i++)
        {
            var member = new LanMemberData();
            member.Deserialize(packetReader, true);
            members[member.MemberId] = member;
        }

        return members;
    }

    /// <summary>
    /// Serializes an RPC packet.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write to.</param>
    /// <param name="channel">The channel the RPC is sent on.</param>
    /// <param name="data">The RPC data payload.</param>
    internal static void SerializeRPC(PacketWriter packetWriter, PacketChannel channel, byte[] data)
    {
        packetWriter.WriteEnum(channel);
        packetWriter.WriteBytesToBuffer(data);
    }

    /// <summary>
    /// Deserializes an RPC packet.
    /// </summary>
    /// <param name="packetReader">The packet reader to read from.</param>
    /// <returns>A tuple containing the channel and data payload.</returns>
    internal static (PacketChannel channel, byte[] data) DeserializeRPC(PacketReader packetReader)
    {
        var channel = packetReader.ReadEnum<PacketChannel>();
        var data = packetReader.GetByteBuffer();
        return (channel, data);
    }

    /// <summary>
    /// Serializes member data for a member.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write to.</param>
    /// <param name="key">The data key.</param>
    /// <param name="value">The data value.</param>
    internal static void SerializeMemberData(PacketWriter packetWriter, string key, string value)
    {
        packetWriter.WriteString(key);
        packetWriter.WriteString(value);
    }

    /// <summary>
    /// Deserializes member data for a member.
    /// </summary>
    /// <param name="packetReader">The packet reader to read from.</param>
    /// <returns>A tuple containing the member ID, key, and value.</returns>
    internal static (string key, string value) DeserializeMemberData(PacketReader packetReader)
    {
        var key = packetReader.ReadString();
        var value = packetReader.ReadString();
        return (key, value);
    }

    /// <summary>
    /// Serializes complete lobby data.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write to.</param>
    /// <param name="serverData">The server data to serialize.</param>
    internal static void SerializeLobbyData(PacketWriter packetWriter, LanServerData serverData)
    {
        serverData.SerializeBroadcast(packetWriter);
    }

    /// <summary>
    /// Deserializes complete lobby data.
    /// </summary>
    /// <param name="packetReader">The packet reader to read from.</param>
    /// <param name="serverData">The server data object to populate.</param>
    internal static void DeserializeLobbyData(PacketReader packetReader, LanServerData serverData)
    {
        serverData.DeserializeBroadcast(packetReader);
    }

    /// <summary>
    /// Serializes a packet header with type information.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write to.</param>
    /// <param name="packetType">The type of server packet.</param>
    internal static void SerializePacketHeader(PacketWriter packetWriter, LanServer.ServerPacket packetType)
    {
        packetWriter.AddTag(PacketHandlerType.Server);
        packetWriter.WriteEnum(packetType);
    }

    /// <summary>
    /// Deserializes a packet header and validates the packet type.
    /// </summary>
    /// <param name="packetReader">The packet reader to read from.</param>
    /// <returns>The server packet type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the packet tag is not Server.</exception>
    internal static LanServer.ServerPacket DeserializePacketHeader(PacketReader packetReader)
    {
        var tag = packetReader.GetTag();
        if (tag != PacketHandlerType.Server)
            throw new InvalidOperationException($"Invalid packet tag: {tag}");

        return packetReader.ReadEnum<LanServer.ServerPacket>();
    }
}
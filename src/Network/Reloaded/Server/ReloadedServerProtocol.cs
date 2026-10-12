using ReplantedOnline.Enums.Network;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Network.Reloaded.Server;

/// <summary>
/// Provides serialization and deserialization methods for IP server protocol packets.
/// </summary>
internal static class ReloadedServerProtocol
{
    internal static void SerializeSetLobbyData(PacketWriter packetWriter, string key, string value, bool remove)
    {
        packetWriter.WriteBool(remove);
        packetWriter.WriteString(key);
        if (!remove)
        {
            packetWriter.WriteString(value);
        }
    }

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

    internal static void SerializeHandshakeRequest(PacketWriter packetWriter, string playerName, ID memberId)
    {
        packetWriter.WriteString(playerName);
        packetWriter.WriteID(memberId);
    }

    internal static (string playerName, ID memberId) DeserializeHandshakeRequest(PacketReader packetReader)
    {
        var playerName = packetReader.ReadString();
        var memberId = packetReader.ReadID();
        return (playerName, memberId);
    }

    internal static void SerializeHandshakeAccept(PacketWriter packetWriter, ID lobbyId)
    {
        packetWriter.WriteID(lobbyId);
    }

    internal static ID DeserializeHandshakeAccept(PacketReader packetReader)
    {
        return packetReader.ReadID();
    }

    internal static void SerializeHandshakeReject(PacketWriter packetWriter, string reason)
    {
        packetWriter.WriteString(reason);
    }

    internal static string DeserializeHandshakeReject(PacketReader packetReader)
    {
        return packetReader.ReadString();
    }

    internal static void SerializeSyncMembers(PacketWriter packetWriter, Dictionary<ID, ReloadedMemberData> members, bool isHandshake)
    {
        packetWriter.WriteBool(isHandshake);
        packetWriter.WritePackedInt(members.Count);
        foreach (var member in members.Values)
        {
            member.Serialize(packetWriter, true);
        }
    }

    internal static (bool IsHandshake, Dictionary<ID, ReloadedMemberData> Members) DeserializeSyncMembers(PacketReader packetReader)
    {
        bool isHandshake = packetReader.ReadBool();
        int memberCount = packetReader.ReadPackedInt();
        var members = new Dictionary<ID, ReloadedMemberData>();

        for (int i = 0; i < memberCount; i++)
        {
            var member = new ReloadedMemberData();
            member.Deserialize(packetReader, true);
            members[member.MemberId] = member;
        }

        return (isHandshake, members);
    }

    internal static void SerializeRPC(PacketWriter packetWriter, PacketChannel channel, byte[] data)
    {
        packetWriter.WriteEnum(channel);
        packetWriter.AddBytesToBuffer(data);
    }

    internal static (PacketChannel channel, byte[] data) DeserializeRPC(PacketReader packetReader)
    {
        var channel = packetReader.ReadEnum<PacketChannel>();
        var data = packetReader.GetBytes();
        return (channel, data);
    }

    internal static void SerializeMemberData(PacketWriter packetWriter, string key, string value)
    {
        packetWriter.WriteString(key);
        packetWriter.WriteString(value);
    }

    internal static (string key, string value) DeserializeMemberData(PacketReader packetReader)
    {
        var key = packetReader.ReadString();
        var value = packetReader.ReadString();
        return (key, value);
    }

    internal static void SerializeLobbyData(PacketWriter packetWriter, ReloadedServerData serverData)
    {
        serverData.SerializeBroadcast(packetWriter);
    }

    internal static void DeserializeLobbyData(PacketReader packetReader, ReloadedServerData serverData)
    {
        serverData.DeserializeBroadcast(packetReader);
    }
}
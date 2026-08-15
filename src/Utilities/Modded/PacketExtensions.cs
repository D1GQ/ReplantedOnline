using Il2CppSteamworks;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Network.Reloaded.Client.Object;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Structs.Reloaded;
using ReplantedOnline.Utilities.MelonLoader;
using System.Net;
using UnityEngine;

namespace ReplantedOnline.Utilities.Modded;

/// <summary>
/// Provides methods for Sterilization for non primary types.
/// </summary>
internal static class PacketExtensions
{
    /// <summary>
    /// Writes a NetworkObject to the packet.
    /// </summary>
    internal static void WriteNetworkObject(this PacketWriter packetWriter, NetworkObject? networkObj)
    {
        if (networkObj != null)
        {
            packetWriter.WriteUInt(networkObj.NetworkId.Id);
        }
        else
        {
            packetWriter.WriteUInt(NetworkObject.NULL);
        }
    }

    /// <summary>
    /// Reads a NetworkObject from the packet.
    /// </summary>
    internal static NetworkObject ReadNetworkObject(this PacketReader packetReader)
    {
        var netId = packetReader.ReadUInt();

        if (netId == NetworkObject.NULL)
        {
            return default!;
        }

        if (ReloadedLobby.LobbyData == null)
        {
            return default!;
        }

        if (ReloadedLobby.LobbyData.NetworkObjectsSpawned.TryGetValue(NetworkIdentifier.Get(netId), out var networkObj))
        {
            return networkObj;
        }

        ReplantedOnlineMod.Logger.Warning(typeof(PacketExtensions), $"ReadNetworkObject has unexpectedly returned default! NetworkId:{netId} not found");

        return default!;
    }

    /// <summary>
    /// Reads a NetworkObject from the packet and attempts to cast it to the specified type T.
    /// </summary>
    internal static T? ReadNetworkObject<T>(this PacketReader packetReader) where T : NetworkObject
    {
        var netObj = packetReader.ReadNetworkObject();
        if (netObj is T typedObj)
            return typedObj;
        return null;
    }

    /// <summary>
    /// Writes an enum value to the packet as an integer.
    /// </summary>
    internal static void WriteEnum<T>(this PacketWriter packetWriter, T value) where T : Enum
    {
        packetWriter.WriteInt(Convert.ToInt32(value));
    }

    /// <summary>
    /// Reads an enum value from the packet as an integer and converts it to the specified enum type.
    /// </summary>
    internal static Enum ReadEnum(this PacketReader packetReader, Type type)
    {
        int value = packetReader.ReadInt();
        return (Enum)Enum.ToObject(type, value);
    }

    /// <summary>
    /// Reads an enum value from the packet as an integer and converts it to the specified enum type.
    /// </summary>
    internal static T ReadEnum<T>(this PacketReader packetReader) where T : Enum
    {
        return (T)packetReader.ReadEnum(typeof(T));
    }

    /// <summary>
    /// Writes a Vector2 to the packet as two consecutive float values (X and Y).
    /// </summary>
    internal static void WriteVector2(this PacketWriter packetWriter, Vector2 value)
    {
        packetWriter.WriteFloat(value.x);
        packetWriter.WriteFloat(value.y);
    }

    /// <summary>
    /// Reads a Vector2 from the packet as two consecutive float values (X and Y).
    /// </summary>
    internal static Vector2 ReadVector2(this PacketReader packetReader)
    {
        float x = packetReader.ReadFloat();
        float y = packetReader.ReadFloat();
        return new Vector2(x, y);
    }

    /// <summary>
    /// Writes an ID to the packet.
    /// </summary>
    internal static void WriteID(this PacketWriter packetWriter, ID id)
    {
        if (id.IsNull)
        {
            packetWriter.WriteByte(0);
            return;
        }

        if (id.IsSteamId && id.TryGetSteamId(out SteamId steamId))
        {
            packetWriter.WriteByte(1);
            packetWriter.WriteULong(steamId);
        }
        else if (id.IsULong && id.TryGetULong(out ulong ulongValue))
        {
            packetWriter.WriteByte(2);
            packetWriter.WriteULong(ulongValue);
        }
        else if (id.IsIPEndPoint && id.TryGetIPEndPoint(out IPEndPoint endpoint))
        {
            packetWriter.WriteByte(3);
            packetWriter.WriteString(endpoint.Address.ToString());
            packetWriter.WriteInt(endpoint.Port);
        }
        else
        {
            packetWriter.WriteByte(0);
        }
    }

    /// <summary>
    /// Reads an ID from the packet.
    /// </summary>
    internal static ID ReadID(this PacketReader packetReader)
    {
        byte type = packetReader.ReadByte();

        switch (type)
        {
            case 0:
                return ID.Null;
            case 1:
                ulong steamIdValue = packetReader.ReadULong();
                return ((SteamId)steamIdValue).AsID();
            case 2:
                ulong uintValue = packetReader.ReadULong();
                return uintValue.AsID();
            case 3:
                string ipString = packetReader.ReadString();
                int port = packetReader.ReadInt();
                if (IPAddress.TryParse(ipString, out var address))
                {
                    return new IPEndPoint(address, port).AsID();
                }
                return ID.Null;
            default:
                return ID.Null;
        }
    }

    /// <summary>
    /// Writes a NetworkId to the packet.
    /// </summary>
    internal static void WriteNetworkId(this PacketWriter packetWriter, NetworkIdentifier networkId)
    {
        packetWriter.WriteUInt(networkId.Id);
    }

    /// <summary>
    /// Reads a NetworkId from the packet.
    /// </summary>
    internal static NetworkIdentifier ReadNetworkId(this PacketReader packetReader)
    {
        return NetworkIdentifier.Get(packetReader.ReadUInt());
    }

    /// <summary>
    /// Writes a BoardUnitX to the packet.
    /// </summary>
    internal static void WriteBoardUnitX(this PacketWriter packetWriter, BoardUnitX value)
    {
        packetWriter.WriteInt(value.Grid);
        packetWriter.WriteFloat(value.Pos);
    }

    /// <summary>
    /// Reads a BoardUnitX from the packet.
    /// </summary>
    internal static BoardUnitX ReadBoardUnitX(this PacketReader packetReader)
    {
        int grid = packetReader.ReadInt();
        float pos = packetReader.ReadFloat();
        return new BoardUnitX(grid, pos);
    }

    /// <summary>
    /// Writes a BoardUnitY to the packet.
    /// </summary>
    internal static void WriteBoardUnitY(this PacketWriter packetWriter, BoardUnitY value)
    {
        packetWriter.WriteInt(value.Grid);
        packetWriter.WriteFloat(value.Pos);
    }

    /// <summary>
    /// Reads a BoardUnitY from the packet.
    /// </summary>
    internal static BoardUnitY ReadBoardUnitY(this PacketReader packetReader)
    {
        int grid = packetReader.ReadInt();
        float pos = packetReader.ReadFloat();
        return new BoardUnitY(grid, pos);
    }
}
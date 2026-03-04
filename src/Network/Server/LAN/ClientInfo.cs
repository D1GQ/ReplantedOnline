using ReplantedOnline.Network.Server.Packet;
using ReplantedOnline.Structs;
using System.Net;

namespace ReplantedOnline.Network.Server.LAN;

/// <summary>
/// Represents information about a client connected to a LAN server.
/// </summary>
internal sealed class ClientInfo
{
    /// <summary>
    /// Gets or sets the unique identifier for this client.
    /// </summary>
    internal ID ClientId { get; set; }

    /// <summary>
    /// Gets or sets the network endpoint (IP address and port) of this client.
    /// </summary>
    internal IPEndPoint EndPoint { get; set; }

    /// <summary>
    /// Gets or sets the display name of this client.
    /// </summary>
    internal string Name { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp of when this client was last seen or heard from.
    /// Used for connection timeout detection.
    /// </summary>
    internal DateTime LastSeen { get; set; }

    /// <summary>
    /// Serializes this client information into a packet writer for network transmission.
    /// </summary>
    /// <param name="writer">The packet writer to serialize data into.</param>
    internal void Serialize(PacketWriter writer)
    {
        writer.WriteID(ClientId);
        writer.WriteID(EndPoint ?? ID.Null);
        writer.WriteString(Name);
    }

    /// <summary>
    /// Deserializes client information from a packet reader.
    /// </summary>
    /// <param name="reader">The packet reader containing serialized client data.</param>
    internal void Deserialize(PacketReader reader)
    {
        ClientId = reader.ReadID();
        var endPointId = reader.ReadID();
        if (endPointId.TryGetIPEndPoint(out var endpoint))
            EndPoint = endpoint;
        Name = reader.ReadString();
    }
}
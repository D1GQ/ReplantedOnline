using ReplantedOnline.Network.Client.Object;

namespace ReplantedOnline.Network.Packet.Messages;

/// <summary>
/// Represents a message used to synchronize the state of a networked object across clients, including its network
/// </summary>
internal sealed class NetworkSyncMessage
{
    /// <summary>
    /// Gets a value indicating whether the initialization process.
    /// </summary>
    public bool Init { get; private set; }

    /// <summary>
    /// Gets the unique network identifier assigned to the spawned object.
    /// </summary>
    public uint NetworkId { get; private set; }

    /// <summary>
    /// Gets the bit field indicating which properties have been modified since the last reset or synchronization.
    /// </summary>
    public uint DirtyBits { get; private set; }

    /// <summary>
    /// Serializes the state of the specified network object into the provided packet writer, including its network
    /// </summary>
    /// <param name="networkObj">The network object whose state is to be serialized. Cannot be null.</param>
    /// <param name="init">A value indicating whether the packet represents an initialization state. If <see langword="true"/>, the packet
    /// will include initialization data.</param>
    /// <param name="packetWriter">The packet writer to which the serialized data will be written. Cannot be null.</param>
    internal static void Serialize(NetworkObject networkObj, bool init, PacketWriter packetWriter)
    {
        packetWriter.WriteUInt(networkObj.NetworkId);
        packetWriter.WriteUInt(networkObj.DirtyBits);
        packetWriter.WriteBool(init);
        networkObj.Serialize(packetWriter, init);
    }

    /// <summary>
    /// Deserializes a network synchronization packet from the specified packet reader.
    /// </summary>
    /// <param name="packetReader">The packet reader from which to read the network synchronization packet data. Must be positioned at the start of
    /// a valid packet.</param>
    /// <returns>A <see cref="NetworkSyncMessage"/> instance containing the deserialized data from the packet reader.</returns>
    internal static NetworkSyncMessage Deserialize(PacketReader packetReader)
    {
        NetworkSyncMessage networkSyncPacket = new()
        {
            NetworkId = packetReader.ReadUInt(),
            DirtyBits = packetReader.ReadUInt(),
            Init = packetReader.ReadBool()
        };

        return networkSyncPacket;
    }
}
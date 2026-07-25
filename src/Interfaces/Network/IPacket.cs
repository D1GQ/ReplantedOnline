namespace ReplantedOnline.Interfaces.Network;

/// <summary>
/// Represents a network packet that can be serialized to a byte buffer.
/// </summary>
internal interface IPacket
{
    /// <summary>
    /// Gets the current packet remaining bytes.
    /// </summary>
    /// <returns>
    /// A new byte array containing the packet remaining byte.
    /// </returns>
    byte[] GetBytes();

    /// <summary>
    /// Gets the current sub-packet remaining bytes.
    /// </summary>
    /// <returns>
    /// A new byte array containing the sub-packet remaining byte.
    /// </returns>
    byte[] GetSubpacketBytes();
}
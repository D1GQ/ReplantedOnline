namespace ReplantedOnline.Interfaces.Network;

/// <summary>
/// Represents a network packet that can be serialized to a byte buffer.
/// </summary>
internal interface IPacket
{
    /// <summary>
    /// Gets the byte buffer representation of the packet.
    /// </summary>
    /// <returns>
    /// A byte array containing the packet data.
    /// </returns>
    byte[] GetByteBuffer();

    /// <summary>
    /// Sets the byte buffer representation of the packet.
    /// </summary>
    /// <param name="buffer">The buffer to set from.</param>
    void SetByteBuffer(byte[] buffer);

    /// <summary>
    /// Scrambles or unscrambles the packet buffer.
    /// </summary>
    void ScrambleBuffer();
}
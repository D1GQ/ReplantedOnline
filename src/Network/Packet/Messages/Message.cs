using ReplantedOnline.Interfaces.Network;

namespace ReplantedOnline.Network.Packet.Messages;

/// <summary>
/// Provides a singleton instance accessor for message types.
/// </summary>
/// <typeparam name="T">The message type that implements <see cref="IMessage"/> and has a parameterless constructor.</typeparam>
internal static class Message<T> where T : IMessage, new()
{
    /// <summary>
    /// Gets the singleton instance of the specified message type.
    /// </summary>
    /// <value>A singleton instance of type <typeparamref name="T"/>.</value>
    internal static T Instance { get; } = new();
}
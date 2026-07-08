using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Interfaces.Network;

namespace ReplantedOnline.Network.Reloaded.Client.Routing;

/// <inheritdoc/>
internal static partial class NetworkManager
{
    /// <summary>
    /// Provides a singleton instance accessor for packet handler types.
    /// </summary>
    /// <typeparam name="T">The packet handler type that implements <see cref="IBasePacketMessage"/>.</typeparam>
    internal abstract class Packet<T> where T : IBasePacketMessage
    {
        /// <summary>
        /// Gets the singleton instance of the specified packet handler type.
        /// </summary>
        /// <value>A singleton instance of type <typeparamref name="T"/> retrieved from the packet registry.</value>
        internal static T Singleton { get; } = RegisterPacketHandler.GetInstance<T>()!;
    }
}
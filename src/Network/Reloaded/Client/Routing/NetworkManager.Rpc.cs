using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Interfaces.Network;

namespace ReplantedOnline.Network.Reloaded.Client.Routing;

/// <inheritdoc/>
internal static partial class NetworkManager
{
    /// <summary>
    /// Provides a singleton instance accessor for RPC handler types.
    /// </summary>
    /// <typeparam name="T">The RPC handler type that implements <see cref="IBaseRpcMessage"/>.</typeparam>
    internal static class Rpc<T> where T : IBaseRpcMessage
    {
        /// <summary>
        /// Gets the singleton instance of the specified RPC handler type.
        /// </summary>
        /// <value>A singleton instance of type <typeparamref name="T"/> retrieved from the RPC registry.</value>
        internal static T Singleton { get; } = RegisterRpc.GetInstance<T>()!;
    }
}
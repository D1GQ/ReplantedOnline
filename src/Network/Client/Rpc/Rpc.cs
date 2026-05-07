using ReplantedOnline.Attributes;
using ReplantedOnline.Interfaces.Network;

namespace ReplantedOnline.Network.Client.Rpc;

/// <summary>
/// Provides a singleton instance accessor for RPC handler types.
/// </summary>
/// <typeparam name="T">The RPC handler type that implements <see cref="IRpc"/>.</typeparam>
internal static class Rpc<T> where T : IRpc
{
    /// <summary>
    /// Gets the singleton instance of the specified RPC handler type.
    /// </summary>
    /// <value>A singleton instance of type <typeparamref name="T"/> retrieved from the RPC registry.</value>
    internal static T Instance { get; } = RegisterRpc.GetInstance<T>();
}
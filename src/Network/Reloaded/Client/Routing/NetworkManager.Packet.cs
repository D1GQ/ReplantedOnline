using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Interfaces.Network;

namespace ReplantedOnline.Network.Reloaded.Client.Routing;

/// <inheritdoc/>
internal static partial class NetworkManager
{
    internal abstract class Packet<T> where T : IBasePacketMessage
    {
        internal static T Singleton { get; } = RegisterPacketHandler.GetInstance<T>()!;
    }
}
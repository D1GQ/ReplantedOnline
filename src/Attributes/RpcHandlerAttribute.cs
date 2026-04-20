using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Client.Object;
using ReplantedOnline.Network.Packet;
using System.Reflection;

namespace ReplantedOnline.Attributes;

/// <summary>
/// Marks a method as an RPC handler for a specific RPC ID.
/// The method will be automatically called when a network message with that RPC ID is received.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class RpcHandlerAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RpcHandlerAttribute"/> class with the specified RPC ID.
    /// Owner check is enabled by default.
    /// </summary>
    /// <param name="rpcId">The RPC ID as a byte value</param>
    internal RpcHandlerAttribute(byte rpcId)
    {
        RpcId = rpcId;
        OwnerCheck = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RpcHandlerAttribute"/> class with the specified RPC ID.
    /// Owner check is enabled by default.
    /// </summary>
    /// <param name="rpcId">The RPC ID (enum or convertible to byte)</param>
    internal RpcHandlerAttribute(object rpcId)
    {
        RpcId = Convert.ToByte(rpcId);
        OwnerCheck = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RpcHandlerAttribute"/> class with the specified RPC ID and owner check setting.
    /// </summary>
    /// <param name="rpcId">The RPC ID as a byte value</param>
    /// <param name="ownerCheck">Whether to check that the sender is the object owner</param>
    internal RpcHandlerAttribute(byte rpcId, bool ownerCheck)
    {
        RpcId = rpcId;
        OwnerCheck = ownerCheck;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RpcHandlerAttribute"/> class with the specified RPC ID and owner check setting.
    /// </summary>
    /// <param name="rpcId">The RPC ID (enum or convertible to byte)</param>
    /// <param name="ownerCheck">Whether to check that the sender is the object owner</param>
    internal RpcHandlerAttribute(object rpcId, bool ownerCheck)
    {
        RpcId = Convert.ToByte(rpcId);
        OwnerCheck = ownerCheck;
    }

    /// <summary>
    /// Gets the RPC ID this handler responds to.
    /// </summary>
    internal byte RpcId { get; }

    /// <summary>
    /// Gets whether to check that the sender is the object owner before executing the handler.
    /// </summary>
    internal bool OwnerCheck { get; }

    private static readonly Dictionary<Type, Dictionary<byte, RpcMethodInfo>> _handlers = [];

    private class RpcMethodInfo
    {
        public MethodInfo Method { get; set; }
        public bool OwnerCheck { get; set; }
    }

    /// <summary>
    /// Provides information about the RPC sender.
    /// </summary>
    internal class RpcInfo
    {
        /// <summary>
        /// Gets the client that sent this RPC.
        /// </summary>
        internal ReplantedClientData Sender { get; set; }
    }

    /// <summary>
    /// Initializes the RPC handler system by scanning all types for methods marked with [RpcHandler].
    /// Call this once when your mod initializes.
    /// </summary>
    internal static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        foreach (var type in assembly.GetTypes().Where(type => !type.IsAbstract && typeof(NetworkObject).IsAssignableFrom(type)))
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attr = method.GetCustomAttribute<RpcHandlerAttribute>();
                if (attr != null)
                {
                    if (!_handlers.ContainsKey(type))
                        _handlers[type] = [];

                    _handlers[type][attr.RpcId] = new RpcMethodInfo
                    {
                        Method = method,
                        OwnerCheck = attr.OwnerCheck
                    };
                }
            }
        }
    }

    /// <summary>
    /// Routes an RPC to the appropriate handler method on the target object.
    /// </summary>
    /// <param name="networkObject">The network object to route the RPC to</param>
    /// <param name="sender">The client that sent the RPC</param>
    /// <param name="rpcId">The ID of the RPC to route</param>
    /// <param name="packetReader">The packet reader containing the RPC data</param>
    internal static void HandleNetworkObjectRpc(NetworkObject networkObject, ReplantedClientData sender, byte rpcId, PacketReader packetReader)
    {
        var type = networkObject.GetType();

        if (!_handlers.TryGetValue(type, out var methods))
            return;

        if (!methods.TryGetValue(rpcId, out var rpcMethodInfo))
            return;

        if (rpcMethodInfo.OwnerCheck && networkObject.OwnerId != sender.ClientId)
            return;

        var method = rpcMethodInfo.Method;
        var parameters = method.GetParameters();
        var args = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;

            if (paramType == typeof(RpcInfo))
            {
                args[i] = new RpcInfo
                {
                    Sender = sender
                };
            }
            else
            {
                args[i] = IFastPacketResolver.ReadFast(packetReader, paramType);
            }
        }

        method.Invoke(networkObject, args);
    }
}
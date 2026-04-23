using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Packet;
using System.Reflection;

namespace ReplantedOnline.Attributes;

/// <summary>
/// Marks a method as an RPC handler for a specific RPC ID.
/// Automatically registers handlers during initialization for all IRpcReceiver types.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class RpcHandlerAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RpcHandlerAttribute"/> class with the specified RPC ID.
    /// Owner check is enabled by default.
    /// </summary>
    /// <param name="rpcId">The RPC ID as a byte value.</param>
    internal RpcHandlerAttribute(byte rpcId)
    {
        RpcId = rpcId;
        OwnerCheck = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RpcHandlerAttribute"/> class with the specified RPC ID.
    /// Owner check is enabled by default.
    /// </summary>
    /// <param name="rpcId">The RPC ID (enum or convertible to byte).</param>
    internal RpcHandlerAttribute(object rpcId)
    {
        RpcId = Convert.ToByte(rpcId);
        OwnerCheck = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RpcHandlerAttribute"/> class with the specified RPC ID and owner check setting.
    /// </summary>
    /// <param name="rpcId">The RPC ID as a byte value.</param>
    /// <param name="ownerCheck">Whether to check that the sender is the object owner before executing the handler.</param>
    internal RpcHandlerAttribute(byte rpcId, bool ownerCheck)
    {
        RpcId = rpcId;
        OwnerCheck = ownerCheck;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RpcHandlerAttribute"/> class with the specified RPC ID and owner check setting.
    /// </summary>
    /// <param name="rpcId">The RPC ID (enum or convertible to byte).</param>
    /// <param name="ownerCheck">Whether to check that the sender is the object owner before executing the handler.</param>
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
    /// When true, RPCs from non-owner clients will be rejected.
    /// </summary>
    internal bool OwnerCheck { get; }

    private static readonly Dictionary<Type, Dictionary<byte, RpcMethodInfo>> _handlers = [];

    /// <summary>
    /// Internal container for cached RPC method reflection information.
    /// </summary>
    private class RpcMethodInfo
    {
        /// <summary>Gets or sets the method info for the RPC handler.</summary>
        public MethodInfo Method { get; set; }

        /// <summary>Gets or sets whether owner validation is required for this handler.</summary>
        public bool OwnerCheck { get; set; }
    }

    /// <summary>
    /// Provides contextual information about the RPC sender to handler methods.
    /// Can be used as a parameter in RPC handler methods to access sender data.
    /// </summary>
    internal class RpcInfo
    {
        /// <summary>
        /// Gets or sets the client data of the client that sent this RPC.
        /// </summary>
        internal ReplantedClientData Sender { get; set; }
    }

    /// <summary>
    /// Initializes the RPC handler system by scanning all assemblies for methods marked with [RpcHandler].
    /// Must be called before any RPCs can be processed.
    /// </summary>
    internal static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        foreach (var type in assembly.GetTypes().Where(type => !type.IsAbstract && typeof(IRpcReceiver).IsAssignableFrom(type)))
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
    /// Routes an RPC to the appropriate handler method on the target IRpcReceiver.
    /// Performs owner validation if required by the handler attribute.
    /// </summary>
    /// <param name="rpcReceiver">The IRpcReceiver instance to route the RPC to.</param>
    /// <param name="sender">The client that sent the RPC.</param>
    /// <param name="rpcId">The ID of the RPC to route.</param>
    /// <param name="packetReader">The packet reader containing the RPC data payload.</param>
    internal static void HandleRpcReceiver(IRpcReceiver rpcReceiver, ReplantedClientData sender, byte rpcId, PacketReader packetReader)
    {
        var type = rpcReceiver.GetType();

        if (!_handlers.TryGetValue(type, out var methods))
            return;

        if (!methods.TryGetValue(rpcId, out var rpcMethodInfo))
            return;

        // Validate ownership if required
        if (rpcMethodInfo.OwnerCheck && rpcReceiver.OwnerId != sender.ClientId)
            return;

        var method = rpcMethodInfo.Method;
        var parameters = method.GetParameters();
        var args = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;

            // Special handling for RpcInfo parameter - inject sender context
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

        method.Invoke(rpcReceiver, args);
    }
}
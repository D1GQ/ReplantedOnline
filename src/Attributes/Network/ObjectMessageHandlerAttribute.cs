using ReplantedOnline.Interfaces;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Network.Reloaded.Serialization;
using System.Reflection;

namespace ReplantedOnline.Attributes.Network;

/// <summary>
/// Marks a method as a message handler for a specific message ID.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class ObjectMessageHandlerAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectMessageHandlerAttribute"/> class with the specified message ID.
    /// Owner check is enabled by default.
    /// </summary>
    /// <param name="message">The message enum value this handler responds to.</param>
    internal ObjectMessageHandlerAttribute(object message)
    {
        Message = (Enum)message;
        OwnerCheck = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectMessageHandlerAttribute"/> class with the specified message ID and owner check setting.
    /// </summary>
    /// <param name="message">The message enum value this handler responds to.</param>
    /// <param name="ownerCheck">Whether to check that the sender is the object owner before executing the handler.</param>
    internal ObjectMessageHandlerAttribute(object message, bool ownerCheck)
    {
        Message = (Enum)message;
        OwnerCheck = ownerCheck;
    }

    /// <summary>
    /// Gets the message enum value that this handler responds to.
    /// </summary>
    internal Enum Message { get; }

    /// <summary>
    /// Gets the message ID assigned to this handler during <see cref="Initialize"/>.
    /// </summary>
    internal uint MessageId { get; private set; }

    /// <summary>
    /// Gets whether to check that the sender is the object owner before executing the handler.
    /// When true, messages from non-owner clients will be rejected.
    /// </summary>
    internal bool OwnerCheck { get; }

    private static readonly Dictionary<Type, Dictionary<Enum, uint>> MessageIdLookup = [];
    private static readonly Dictionary<Type, Dictionary<Enum, Type[]>> ArgsTypesLookup = [];
    private static readonly Dictionary<Type, Dictionary<uint, MessageMethodInfo>> HandlerLookup = [];

    /// <summary>
    /// Gets the message ID for the specified message enum value, if it has been registered.
    /// </summary>
    /// <param name="catchedType">The <see cref="ICatchedType"/> instance to look up.</param>
    /// <param name="rpc">The message enum value to look up.</param>
    /// <param name="rpcId">When this method returns, contains the assigned message ID if found; otherwise, 0.</param>
    /// <returns><see langword="true"/> if the message enum was registered; otherwise, <see langword="false"/>.</returns>
    internal static bool TryGetMessageId(ICatchedType catchedType, Enum rpc, out uint rpcId)
    {
        rpcId = 0;
        return MessageIdLookup.TryGetValue(catchedType.Type, out var lookup)
            && lookup.TryGetValue(rpc, out rpcId);
    }

    /// <summary>
    /// Gets the argument types for the specified message enum value, caching the result for future lookups.
    /// </summary>
    /// <param name="catchedType">The <see cref="ICatchedType"/> instance to look up.</param>
    /// <param name="rpc">The message enum value to look up.</param>
    /// <returns>An array of <see cref="Type"/> representing the argument types.</returns>
    internal static Type[] GetMessageArgsTypes(ICatchedType catchedType, Enum rpc)
    {
        if (ArgsTypesLookup.TryGetValue(catchedType.Type, out var lookup) && lookup.TryGetValue(rpc, out var argsTypes))
        {
            return argsTypes;
        }

        throw new InvalidOperationException($"Message method info not found for message '{rpc}' in type '{catchedType.Type.FullName}'.");
    }

    /// <summary>
    /// Initializes the message handler system.
    /// </summary>
    internal static void Initialize()
    {
        var types = ReplantedOnlineMod.ModInfo.Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(INetworkIdentifier).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal);

        foreach (var type in types)
        {
            var methods = type
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .OrderBy(m => m.Name, StringComparer.Ordinal)
                .ThenBy(m => m.MetadataToken);

            uint nextRpcId = 0;

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<ObjectMessageHandlerAttribute>();
                if (attr == null)
                    continue;

                attr.MessageId = nextRpcId++;
                var rpcMethodInfo = new MessageMethodInfo(method)
                {
                    OwnerCheck = attr.OwnerCheck
                };

                if (!MessageIdLookup.TryGetValue(type, out var rpcIds))
                    MessageIdLookup[type] = rpcIds = [];
                rpcIds[attr.Message] = attr.MessageId;

                if (!HandlerLookup.TryGetValue(type, out var handlers))
                    HandlerLookup[type] = handlers = [];
                handlers[attr.MessageId] = rpcMethodInfo;

                if (!ArgsTypesLookup.TryGetValue(type, out var argsTypes))
                    ArgsTypesLookup[type] = argsTypes = [];
                argsTypes[attr.Message] = [.. rpcMethodInfo.Parameters.Select(p => p.ParameterType)];
            }
        }
    }

    /// <summary>
    /// Routes a message to the appropriate handler method on the target <see cref="INetworkIdentifier"/>.
    /// </summary>
    /// <param name="networkIdentifier">The <see cref="INetworkIdentifier"/> instance to route the message to.</param>
    /// <param name="catchedType">The <see cref="ICatchedType"/> instance to route the message to.</param>
    /// <param name="sender">The client that sent the message.</param>
    /// <param name="rpcId">The ID of the message to route.</param>
    /// <param name="packetReader">The packet reader containing the message data payload.</param>
    internal static void HandleObjectMessage(INetworkIdentifier networkIdentifier, ICatchedType catchedType, ReloadedClientData sender, uint rpcId, PacketReader packetReader)
    {
        var type = catchedType.Type;

        if (!HandlerLookup.TryGetValue(type, out var handlers))
            return;

        if (!handlers.TryGetValue(rpcId, out var rpcMethodInfo))
            return;

        if (rpcMethodInfo.OwnerCheck && networkIdentifier.OwnerId != sender.ClientId)
            return;

        var parameters = rpcMethodInfo.Parameters;
        var args = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;

            args[i] = paramType == typeof(MessageInfo)
                ? new MessageInfo { Sender = sender }
                : IFastPacketResolver.ReadFast(packetReader, paramType);
        }

        rpcMethodInfo.Method.Invoke(networkIdentifier, args);
    }

    /// <summary>
    /// Provides contextual information about the message sender to handler methods.
    /// </summary>
    internal sealed class MessageInfo
    {
        /// <summary>
        /// Gets or sets the client data of the client that sent this message.
        /// </summary>
        internal ReloadedClientData Sender { get; set; } = null!;
    }

    /// <summary>
    /// Internal container for cached message method reflection information.
    /// </summary>
    private sealed class MessageMethodInfo(MethodInfo methodInfo)
    {
        /// <summary>
        /// Gets the method info for the message handler.
        /// </summary>
        public readonly MethodInfo Method = methodInfo;

        /// <summary>
        /// Gets the parameters of the message handler method.
        /// </summary>
        public readonly ParameterInfo[] Parameters = methodInfo.GetParameters();

        /// <summary>
        /// Gets whether owner validation is required for this handler.
        /// </summary>
        public bool OwnerCheck { get; init; }
    }
}
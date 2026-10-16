using ReplantedOnline.Attributes.Hook;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Reloaded.Client;
using ReplantedOnline.Network.Reloaded.Client.Routing;
using ReplantedOnline.Network.Reloaded.Client.Routing.Packet;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Utilities.MelonLoader;
using System.Reflection;
using System.Text;

namespace ReplantedOnline.Attributes.Network;

/// <summary>
/// Marks a method as a network RPC. When the marked method is invoked, the call is
/// serialized and sent to the corresponding instance on remote clients, where the original method is then executed locally.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class MethodRpc : MethodAutoHookAttribute<MethodRpc, INetworkIdentifier>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MethodRpc"/> attribute.
    /// </summary>
    /// <param name="checkOwner">
    /// If <see langword="true"/>, the RPC is only sent and processed when the
    /// invoking client owns the target network identifier.
    /// </param>
    /// <param name="localExecution">
    /// Specifies whether the original method should run locally, and if so,
    /// whether it runs before or after the RPC is sent.
    /// </param>
    internal MethodRpc(bool checkOwner, LocalExecution localExecution = LocalExecution.None)
    {
        CheckOwner = checkOwner;
        LocalExecutionType = localExecution;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MethodRpc"/> attribute with
    /// owner checking enabled.
    /// </summary>
    /// <param name="localExecution">
    /// Specifies whether the original method should run locally, and if so,
    /// whether it runs before or after the RPC is sent.
    /// </param>
    internal MethodRpc(LocalExecution localExecution)
    {
        CheckOwner = true;
        LocalExecutionType = localExecution;
    }

    /// <summary>
    /// Gets the mode that determines when the original method executes locally.
    /// </summary>
    internal readonly LocalExecution LocalExecutionType;

    /// <summary>
    /// Gets a value indicating whether ownership of the network identifier is
    /// verified before sending or processing this RPC.
    /// </summary>
    internal readonly bool CheckOwner;

    /// <summary>
    /// Carries per-invocation context for an RPC, such as whether the call is
    /// local and, if so, hooks that can cancel or react to the send.
    /// </summary>
    internal sealed record RpcInfo(bool IsLocal, ReloadedClientData? Sender)
    {
        /// <summary>
        /// Holds state that is only relevant when the RPC is invoked locally.
        /// </summary>
        internal readonly LocalRpcInfo Local = new();

        /// <summary>
        /// Mutable state associated with a locally invoked RPC.
        /// </summary>
        internal sealed class LocalRpcInfo
        {
            /// <summary>
            /// The arguments passed to the rpc.
            /// </summary>
            internal object[] Args = [];

            /// <summary>
            /// When set to <see langword="true"/>, prevents the RPC from being sent.
            /// </summary>
            internal bool CancelSend;

            /// <summary>
            /// An optional callback invoked after the RPC has been sent.
            /// </summary>
            internal Action? AfterSend;
        }
    }

    /// <summary>
    /// Associates a registered RPC identifier with its method and attribute metadata.
    /// </summary>
    private sealed class RpcMethod(uint id, HookEntry hookEntry, MethodRpc attribute)
    {
        /// <summary>
        /// The unique identifier assigned to this RPC.
        /// </summary>
        public readonly uint Id = id;

        /// <summary>
        /// The HookEntry that implements this RPC.
        /// </summary>
        public readonly HookEntry HookEntry = hookEntry;

        /// <summary>
        /// The method that implements this RPC.
        /// </summary>
        public readonly MethodInfo Method = hookEntry.MethodInfo;

        /// <summary>
        /// The attribute instance that declared this RPC.
        /// </summary>
        public readonly MethodRpc Attribute = attribute;

        /// <summary>
        /// The cached parameter list.
        /// </summary>
        public readonly ParameterInfo[] Parameters = hookEntry.MethodInfo.GetParameters();
    }

    private static readonly Dictionary<uint, RpcMethod> RpcMethodLookupById = [];
    private static readonly Dictionary<MethodInfo, RpcMethod> RpcMethodLookupByMethod = [];

    /// <summary>
    /// Computes a stable 32-bit identifier for an RPC method based on its
    /// declaring type, name, and parameter types.
    /// </summary>
    /// <param name="method">The RPC method to identify.</param>
    /// <returns>The computed identifier.</returns>
    private static uint ComputeId(MethodInfo method)
    {
        var sb = new StringBuilder()
            .Append(method.DeclaringType!.FullName)
            .Append("::")
            .Append(method.Name)
            .Append('(');

        foreach (var p in method.GetParameters())
            sb.Append(p.ParameterType.FullName).Append(',');

        sb.Append(')');

        uint hash = 2166136261;
        foreach (char c in sb.ToString())
        {
            hash ^= c;
            hash *= 16777619;
        }

        return hash;
    }

    /// <inheritdoc/>
    protected override bool ShouldInstallHook(HookEntry hookEntry)
    {
        var method = hookEntry.MethodInfo;

        if (method.IsStatic || method.IsGenericMethod)
            return false;

        var id = ComputeId(method);
        if (RpcMethodLookupById.TryGetValue(id, out var clash))
        {
            throw new InvalidOperationException(
                $"RPC id collision (0x{id:X8}): {method.DeclaringType!.Name}.{method.Name} vs " +
                $"{clash.Method.DeclaringType!.Name}.{clash.Method.Name}. Rename one of them.");
        }

        var rpc = new RpcMethod(id, hookEntry, this);
        RpcMethodLookupById[id] = rpc;
        RpcMethodLookupByMethod[method] = rpc;
        return true;
    }

    /// <inheritdoc/>
    protected override object? Hook(HookEntry hookEntry, INetworkIdentifier? instance, object[] args, OriginalDelegate original)
    {
        if (instance == null)
            return null;

        if (CheckOwner && instance.OwnerId != ReloadedClientData.LocalClient!.ClientId)
            return null;

        RpcInfo? rpcInfo = null;
        var parameters = hookEntry.MethodInfo.GetParameters();
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType == typeof(RpcInfo))
            {
                rpcInfo = new RpcInfo(true, null)
                {
                    Local = { Args = args }
                };
                args[i] = rpcInfo;
            }
        }

        ObjectRpcPacket.CatchedNetworkIdentifier catchedInstance = new(instance);
        object? result = null;

        if (LocalExecutionType == LocalExecution.Before)
        {
            result = original(instance, args);

            if (rpcInfo != null && rpcInfo.Local.CancelSend)
                return result;
        }

        if (RpcMethodLookupByMethod.TryGetValue(hookEntry.MethodInfo, out var rpc))
        {
            SendRpc(catchedInstance, rpc, args);
        }
        else
        {
            ReplantedOnlineMod.Logger.Warning(
                typeof(MethodRpc),
                $"RPC {hookEntry.Type.Name}.{hookEntry.MethodInfo.Name} is not registered.");
        }

        if (LocalExecutionType == LocalExecution.After)
            result = original(instance, args);

        rpcInfo?.Local.AfterSend?.Invoke();

        return result;
    }

    /// <summary>
    /// Serializes the specified arguments and sends the RPC to remote clients.
    /// </summary>
    /// <param name="catchedNetworkIdentifier">The target network identifier.</param>
    /// <param name="rpc">The RPC metadata describing the method to invoke.</param>
    /// <param name="args">The arguments to serialize.</param>
    private static void SendRpc(ObjectRpcPacket.CatchedNetworkIdentifier catchedNetworkIdentifier, RpcMethod rpc, object[] args)
    {
        var payload = PacketWriter.Get();

        try
        {
            for (int i = 0; i < rpc.Parameters.Length; i++)
            {
                var paramType = rpc.Parameters[i].ParameterType;
                if (paramType == typeof(RpcInfo))
                    continue;

                IFastPacketResolver.WriteFast(payload, args[i], paramType);
            }

            NetworkManager.Packet<ObjectRpcPacket>.Singleton.Send(catchedNetworkIdentifier, rpc.Id, payload);
            ReplantedOnlineMod.Logger.Msg(
                typeof(MethodRpc),
                $"Sent Object RPC: {rpc.Method.DeclaringType!.Name}.{rpc.Method.Name} for NetworkId: {catchedNetworkIdentifier.NetworkId}");
        }
        finally
        {
            payload.Recycle();
        }
    }

    /// <summary>
    /// Handles an incoming RPC by deserializing its arguments and invoking the original method on the local network identifier.
    /// </summary>
    /// <param name="networkIdentifier">The local network identifier targeted by the RPC.</param>
    /// <param name="catchedType">The caught type associated with the identifier.</param>
    /// <param name="sender">The client that sent the RPC.</param>
    /// <param name="rpcId">The identifier of the RPC to invoke.</param>
    /// <param name="packetReader">The reader positioned at the serialized arguments.</param>
    internal static void HandleRpc(INetworkIdentifier? networkIdentifier, ICatchedType? catchedType, ReloadedClientData sender, uint rpcId, PacketReader packetReader)
    {
        if (!RpcMethodLookupById.TryGetValue(rpcId, out var rpc))
            return;

        if (networkIdentifier == null || catchedType == null)
        {
            ReplantedOnlineMod.Logger.Error(
                typeof(MethodRpc),
                $"Error processing Object RPC from {sender.Name}: {rpc.Method.DeclaringType!.Name}.{rpc.Method.Name}, NetworkIdentifier is null!");
            return;
        }

        if (!rpc.Method.DeclaringType!.IsAssignableFrom(catchedType.CatchedType))
            return;

        if (rpc.Attribute.CheckOwner && networkIdentifier.OwnerId != sender.ClientId)
            return;

        var original = GetOriginal(rpc.HookEntry);
        if (original is null)
            return;

        var parameters = rpc.Parameters;
        var args = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;

            args[i] = paramType == typeof(RpcInfo)
                ? new RpcInfo(false, sender)
                : IFastPacketResolver.ReadFast(packetReader, paramType);
        }

        ReplantedOnlineMod.Logger.Msg(
            typeof(MethodRpc),
            $"Processing Object RPC from {sender.Name}: {rpc.Method.DeclaringType!.Name}.{rpc.Method.Name} for NetworkId: {networkIdentifier.NetworkId}");

        original(networkIdentifier, args);
    }
}
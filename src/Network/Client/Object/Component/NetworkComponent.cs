using Il2CppInterop.Runtime.Attributes;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Routing;
using ReplantedOnline.Structs;

namespace ReplantedOnline.Network.Client.Object.Component;

/// <summary>
/// Abstract base class for network components that provide modular functionality to NetworkObjects.
/// Components handle serialization, updates, and RPC communication for specific features.
/// </summary>
internal abstract class NetworkComponent : IRpcReceiver
{
    /// <summary>
    /// Gets or sets the NetworkObject that owns this component.
    /// Can only be set once; subsequent assignment attempts are ignored.
    /// </summary>
    internal NetworkObject NetworkObject
    {
        get;
        set
        {
            if (field != null) return;
            field = value;
        }
    }

    /// <summary>
    /// Gets the identifier of the client that owns the parent NetworkObject.
    /// </summary>
    public ID OwnerId => NetworkObject.OwnerId;

    /// <summary>
    /// Gets the unique network identifier of the parent NetworkObject.
    /// </summary>
    public uint NetworkId => NetworkObject.NetworkId;

    /// <summary>
    /// Gets or sets the index of this component within its parent NetworkObject's component collection.
    /// </summary>
    internal int Index { get; set; }

    /// <summary>
    /// Called after the network component is added to a network object.
    /// </summary>
    internal virtual void Init() { }

    /// <summary>
    /// Called after Network Object has been set active.
    /// </summary>
    internal virtual void OnEnabled() { }

    /// <summary>
    /// Called every frame to update component logic.
    /// </summary>
    internal virtual void Update() { }

    /// <summary>
    /// Serializes component state into a packet for network transmission.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write serialized data to.</param>
    /// <param name="init">If true, performs full initialization serialization; if false, performs delta/dirty serialization.</param>
    internal virtual void Serialize(PacketWriter packetWriter, bool init) { }

    /// <summary>
    /// Deserializes component state from a received network packet.
    /// </summary>
    /// <param name="packetReader">The packet reader containing serialized state data.</param>
    /// <param name="init">If true, performs full initialization deserialization; if false, performs delta/dirty deserialization.</param>
    internal virtual void Deserialize(PacketReader packetReader, bool init) { }

    /// <summary>
    /// Sends a Remote Procedure Call (RPC) for this network component.
    /// </summary>
    /// <typeparam name="T">The enum type of the RPC identifier.</typeparam>
    /// <param name="rpcId">The RPC identifier to send.</param>
    /// <param name="args">Optional arguments to pass to the RPC handler.</param>
    [HideFromIl2Cpp]
    public void SendNetworkComponentRpc<T>(T rpcId, params object[] args) where T : Enum
    {
        PacketWriter packetWriter = null;
        if (args.Length > 0)
        {
            packetWriter = PacketWriter.Get();
            foreach (var arg in args)
            {
                IFastPacketResolver.WriteFast(packetWriter, arg, arg?.GetType() ?? typeof(NetworkObject));
            }
        }
        NetworkDispatcher.SendRpcReceiver(this, Convert.ToByte(rpcId), packetWriter);
        packetWriter?.Recycle();
    }
}
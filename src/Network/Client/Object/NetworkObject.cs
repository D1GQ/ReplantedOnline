using Il2CppInterop.Runtime.Attributes;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Modules;
using ReplantedOnline.Monos;
using ReplantedOnline.Network.Client.Object.Component;
using ReplantedOnline.Network.Client.Object.Replanted;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Routing;
using ReplantedOnline.Structs;

namespace ReplantedOnline.Network.Client.Object;

/// <summary>
/// Abstract Base class for all network-synchronized objects in ReplantedOnline.
/// Provides core functionality for ownership, synchronization, and remote procedure calls.
/// </summary>
internal abstract class NetworkObject : RuntimePrefab, INetworkObject, IRpcReceiver
{
    /// <summary>
    /// Represents a null or uninitialized value.
    /// </summary>
    internal const uint NULL = uint.MaxValue;

    /// <summary>
    /// Gets the parent network object associated with this instance.
    /// </summary>
    [HideFromIl2Cpp]
    internal NetworkObject ParentNetworkObject { get; private set; }

    /// <summary>
    /// if this NetworkObject is a child of another NetworkObject
    /// </summary>
    internal bool AmChild { get; private set; }

    /// <summary>
    /// Contains the collection of child network objects associated with this instance.
    /// </summary>
    [HideFromIl2Cpp]
    internal List<NetworkObject> ChildNetworkObjects { get; } = [];


    /// <summary>
    /// Contains the collection of network object components associated with this instance.
    /// </summary>
    [HideFromIl2Cpp]
    internal List<NetworkComponent> NetworkComponents { get; } = [];

    /// <summary>
    /// Contains the lookups of network object components associated with this instance.
    /// </summary>
    private readonly Dictionary<Type, NetworkComponent> _networkComponentsLookup = [];

    /// <summary>
    /// Dictionary of registered network prefabs that can be spawned across the network.
    /// Key is the prefab ID, value is the NetworkObject prefab reference.
    /// </summary>
    internal static readonly Dictionary<byte, NetworkObject> NetworkPrefabs = [];

    /// <summary>
    /// Dictionary of registered network prefabs that can be spawned across the network.
    /// Key is the prefab ID, value is the NetworkObject prefab reference.
    /// </summary>
    internal static readonly Dictionary<Type, byte> PrefabIdTypeLookup = [];

    /// <summary>
    /// Constant value representing no prefab ID, used for dynamically created network objects.
    /// </summary>
    internal const byte NO_PREFAB_ID = byte.MinValue;

    /// <summary>
    /// Gets or sets the synchronization bits tracker for this network object.
    /// Manages which properties need to be synchronized across the network.
    /// </summary>
    [HideFromIl2Cpp]
    public SyncedBits SyncedBits { get; set; } = new SyncedBits();

    /// <summary>
    /// Gets or sets the prefab identifier for this network object.
    /// Used to identify which prefab to instantiate for spawned objects.
    /// </summary>
    public byte PrefabId { get; set; } = NO_PREFAB_ID;

    /// <summary>
    /// Gets whether the local client is the owner of this network object.
    /// Determines if this client has authority to modify the object's state.
    /// </summary>
    internal bool AmOwner => ReplantedLobby.NetworkTransport.LocalClientId == OwnerId;

    /// <summary>
    /// Gets or sets the Steam ID of the client who owns this network object.
    /// The owner has authority over the object's state and behavior.
    /// </summary>
    public ID OwnerId { get; set; } = ID.Null;

    /// <summary>
    /// Gets or sets whether this network object has been successfully spawned across the network.
    /// Indicates if the object is currently active and synchronized with other clients.
    /// </summary>
    internal bool IsOnNetwork { get; set; }

    /// <summary>
    /// Gets or sets whether this network object is ready to despawn locally.
    /// </summary>
    internal bool IsReadyToDespawn { get; set; }

    /// <summary>
    /// Gets or sets the unique network identifier for this object.
    /// Used to reference this specific object across all connected clients.
    /// </summary>
    public uint NetworkId { get; set; } = 0;

    /// <summary>
    /// Gets or sets the dirty bits flag indicating modified properties.
    /// Each bit represents whether a specific property has changed since last sync.
    /// </summary>
    public uint DirtyBits { get; set; }

    /// <summary>
    /// Gets whether any properties are dirty and need synchronization.
    /// Returns true if any bits in DirtyBits are set.
    /// </summary>
    internal bool IsDirty => DirtyBits > 0U;

    /// <summary>
    /// Checks if a specific dirty bit is set at the given index.
    /// </summary>
    /// <param name="idx">The zero-based index of the bit to check.</param>
    /// <returns>True if the bit at the specified index is set.</returns>
    internal bool IsDirtyBitSet(int idx)
    {
        return (DirtyBits & 1U << idx) > 0U;
    }

    /// <summary>
    /// Clears all dirty bits, marking all properties as synchronized.
    /// </summary>
    internal void ClearDirtyBits()
    {
        DirtyBits = 0U;
    }

    /// <summary>
    /// Unsets a specific dirty bit at the given index.
    /// Marks a property as no longer needing synchronization.
    /// </summary>
    /// <param name="idx">The zero-based index of the bit to unset.</param>
    internal void UnsetDirtyBit(int idx)
    {
        DirtyBits &= ~(1U << idx);
    }

    /// <summary>
    /// Sets a specific dirty bit at the given index.
    /// Marks a property as modified and needing synchronization.
    /// </summary>
    /// <param name="idx">The zero-based index of the bit to set (0-31).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when index is outside 0-31 range.</exception>
    internal void SetDirtyBit(int idx)
    {
        if (idx < 0 || idx >= 32)
        {
            throw new ArgumentOutOfRangeException(nameof(idx), "Index must be between 0 and 31.");
        }

        DirtyBits |= 1U << idx;
    }

    /// <summary>
    /// Marks the object as dirty by setting a default dirty bit.
    /// </summary>
    new internal void MarkDirty()
    {
        SetDirtyBit(1);
    }

    /// <summary>
    /// Called when the network object is first initialized on the client side.
    /// </summary>
    public virtual void OnInit() { }

    /// <summary>
    /// Called after Network Object has been set active.
    /// </summary>
    public virtual void OnEnabled()
    {
        foreach (var component in NetworkComponents)
        {
            component.OnEnabled();
        }
    }

    /// <summary>
    /// Called when the network object is being despawned/removed on the client side.
    /// </summary>
    public virtual void OnDespawn() { }

    /// <summary>
    /// Gets the name to set the GameObject when the network object is spawned.
    /// </summary>
    /// <returns></returns>
    public virtual string GetObjectName()
    {
        return GetType().Name + $" ({NetworkId})";
    }

    /// <summary>
    /// Sends an RPC (Remote Procedure Call) for this network object.
    /// </summary>
    /// <typeparam name="T">The enum type of the RPC identifier.</typeparam>
    /// <param name="rpcId">The RPC identifier to send.</param>
    /// <param name="args">Optional arguments to pass to the RPC handler.</param>
    [HideFromIl2Cpp]
    public void SendNetworkObjectRpc<T>(T rpcId, params object[] args) where T : Enum
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

    /// <summary>
    /// Serializes the object's state for network transmission.
    /// Override this method to implement custom serialization logic.
    /// </summary>
    /// <param name="packetWriter">The packet writer to serialize data into.</param>
    /// <param name="init">Whether this is initial serialization or update serialization.</param>
    [HideFromIl2Cpp]
    public virtual void Serialize(PacketWriter packetWriter, bool init) { }

    /// <summary>
    /// Deserializes the object's state from network data.
    /// Override this method to implement custom deserialization logic.
    /// </summary>
    /// <param name="packetReader">The packet reader to deserialize data from.</param>
    /// <param name="init">Whether this is initial deserialization or update deserialization.</param>
    [HideFromIl2Cpp]
    public virtual void Deserialize(PacketReader packetReader, bool init) { }

    /// <summary>
    /// Adds a child NetworkObject to this instance's collection of child network objects.
    /// This operation is only permitted before the object has been spawned in the network.
    /// </summary>
    [HideFromIl2Cpp]
    internal void AddChild(NetworkObject child)
    {
        if (IsOnNetwork) return;

        child.AmChild = true;
        child.ParentNetworkObject = this;
        ChildNetworkObjects.Add(child);
    }

    /// <summary>
    /// Adds a new network component to this instance's collection of network components.
    /// </summary>
    /// <typeparam name="T">The type of  NetworkComponent to create and add.</typeparam>
    [HideFromIl2Cpp]
    internal T AddNetworkComponent<T>() where T : NetworkComponent
    {
        var type = typeof(T);

        if (_networkComponentsLookup.ContainsKey(type))
        {
            throw new Exception($"NetworkObject already contains a component with the type: {type.Name}");
        }

        T component = Activator.CreateInstance(typeof(T)) as T;
        component.NetworkObject = this;
        component.Index = NetworkComponents.Count;
        NetworkComponents.Add(component);
        _networkComponentsLookup[type] = component;
        component.Init();
        return component;
    }

    /// <summary>
    /// Gets network component from this instance's collection of network components by its type.
    /// </summary>
    /// <typeparam name="T">The type of  NetworkComponent to get.</typeparam>
    internal T GetNetworkComponent<T>() where T : NetworkComponent
    {
        var type = typeof(T);
        if (_networkComponentsLookup.TryGetValue(type, out var component))
        {
            return component as T;
        }

        return null;
    }

    /// <summary>
    /// Trys to get network component from this instance's collection of network components by its type.
    /// </summary>
    /// <typeparam name="T">The type of  NetworkComponent to get.</typeparam>
    internal bool TryGetNetworkComponent<T>(out T component) where T : NetworkComponent
    {
        component = GetNetworkComponent<T>();
        return component != null;
    }

    /// <summary>
    /// Spawns a new instance of a NetworkObject-derived type across the network.
    /// Creates the object locally and broadcasts spawn notification to all clients.
    /// </summary>
    /// <typeparam name="T">The type of NetworkObject to spawn, must derive from NetworkObject.</typeparam>
    /// <param name="callback">Optional callback to configure the object before spawning.</param>
    /// <param name="owner">The Steam ID of the owner who controls this network object.</param>
    /// <returns>The newly spawned NetworkObject instance.</returns>
    public static T SpawnNew<T>(Action<T> callback = default, ID owner = default) where T : NetworkObject
    {
        if (PrefabIdTypeLookup.TryGetValue(typeof(T), out var prefabId))
        {
            if (NetworkPrefabs.TryGetValue(prefabId, out var prefab))
            {
                T networkObj = prefab.Clone<T>();
                networkObj.transform.SetParent(GlobalGameObjects.NetworkObjectsGo.transform);
                callback?.Invoke(networkObj);
                networkObj.OnInit();
                NetworkDispatcher.SpawnNetworkObject(networkObj, owner);
                networkObj.gameObject.SetActive(true);
                networkObj.OnEnabled();
                return networkObj;
            }
        }

        return null;
    }

    /// <summary>
    /// Despawns the network object and removes it from all connected clients.
    /// Also destroys the associated game object.
    /// </summary>
    /// <param name="waitToBeReady">Indicate whether the network object should wait until locally want to despawn on the other side .</param>
    public void DespawnAndDestroy(bool waitToBeReady = false)
    {
        if (!AmOwner || AmChild) return;

        Despawn(waitToBeReady);
        Destroy(gameObject);
    }

    /// <summary>
    /// Despawns the network object and removes it from all connected clients.
    /// Cleans up network resources and sends despawn notification to other clients.
    /// </summary>
    /// <param name="waitToBeReady">Indicate whether the network object should wait until locally want to despawn on the other side .</param>
    public void Despawn(bool waitToBeReady = false)
    {
        if (!AmOwner || AmChild) return;

        if (IsOnNetwork)
        {
            NetworkDispatcher.DespawnNetworkObject(this, waitToBeReady);
        }
    }

    /// <summary>
    /// Initializes and registers network prefabs used for object spawning across the network.
    /// This method sets up predefined prefab templates that can be instantiated and synchronized
    /// between clients during multiplayer sessions.
    /// </summary>
    internal static void SetupPrefabs()
    {
        CreateNetworkPrefab<PlantNetworked>(1);
        CreateNetworkPrefab<ZombieNetworked>(2);
    }

    /// <summary>
    /// Creates and registers a network prefab of the specified type with a unique identifier.
    /// The prefab is marked as hidden and persistent, serving as a template for network instantiation.
    /// </summary>
    private static T CreateNetworkPrefab<T>(byte prefabId, Action<T> callback = null) where T : NetworkObject
    {
        var networkObject = CreatePrefab<T>($"{typeof(T)}:{prefabId}");
        networkObject.transform.SetParent(GlobalGameObjects.NetworkPrefabsObj.transform);
        callback?.Invoke(networkObject);
        networkObject.PrefabId = prefabId;
        NetworkPrefabs[prefabId] = networkObject;
        PrefabIdTypeLookup[typeof(T)] = prefabId;
        return networkObject;
    }
}
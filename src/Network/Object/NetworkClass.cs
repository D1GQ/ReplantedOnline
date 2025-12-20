using Il2CppInterop.Runtime.Attributes;
using Il2CppSteamworks;
using ReplantedOnline.Enums;
using ReplantedOnline.Interfaces;
using ReplantedOnline.Monos;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Packet;
using UnityEngine;

namespace ReplantedOnline.Network.Object;

/// <summary>
/// Abstract Base class for all network-synchronized objects in ReplantedOnline.
/// Provides core functionality for ownership, synchronization, and remote procedure calls.
/// </summary>
internal abstract class NetworkClass : RuntimePrefab, INetworkClass
{
    /// <summary>
    /// Gets the parent network class associated with this instance.
    /// </summary>
    [HideFromIl2Cpp]
    internal NetworkClass ParentNetworkClass { get; private set; }

    /// <summary>
    /// if this NetworkClass is a child of another NetworkClass
    /// </summary>
    internal bool AmChild { get; private set; }

    /// <summary>
    /// Contains the collection of child network classes associated with this instance.
    /// </summary>
    [HideFromIl2Cpp]
    internal List<NetworkClass> ChildNetworkClasses { get; } = [];

    /// <summary>
    /// Container GameObject for all network prefabs.
    /// </summary>
    private static GameObject NetworkPrefabsObj;

    /// <summary>
    /// Container GameObject for all network classes
    /// </summary>
    internal static GameObject NetworkClassesObj
    {
        get
        {
            if (_networkClassesObj == null)
            {
                _networkClassesObj = new GameObject("NetworkClasses");
            }

            return _networkClassesObj;
        }
    }

    /// <summary>
    /// Base container GameObject for all network classes
    /// </summary>
    private static GameObject _networkClassesObj;

    /// <summary>
    /// Dictionary of registered network prefabs that can be spawned across the network.
    /// Key is the prefab ID, value is the NetworkClass prefab reference.
    /// </summary>
    internal static readonly Dictionary<byte, NetworkClass> NetworkPrefabs = [];

    /// <summary>
    /// Dictionary of registered network prefabs that can be spawned across the network.
    /// Key is the prefab ID, value is the NetworkClass prefab reference.
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
    internal bool AmOwner => SteamUser.Internal.GetSteamID() == OwnerId;

    /// <summary>
    /// Gets or sets the Steam ID of the client who owns this network object.
    /// The owner has authority over the object's state and behavior.
    /// </summary>
    public SteamId OwnerId { get; set; } = default;

    /// <summary>
    /// Gets or sets whether this network object has been successfully spawned across the network.
    /// Indicates if the object is currently active and synchronized with other clients.
    /// </summary>
    internal bool IsOnNetwork { get; set; }

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
    /// Called after successful network synchronization.
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
    /// Forces the object to be synchronized on the next network update.
    /// </summary>
    new internal void MarkDirty()
    {
        SetDirtyBit(1);
    }

    /// <summary>
    /// Called when the network object is first initialized on the client side.
    /// Override this method to implement client-side initialization logic.
    /// </summary>
    public virtual void OnSpawn() { }

    /// <summary>
    /// Called when the network object is being despawned/removed on the client side.
    /// Override this method to implement client-side cleanup logic before the object is destroyed.
    /// </summary>
    public virtual void OnDespawn() { }

    /// <summary>
    /// Handles incoming Remote Procedure Calls for this network object.
    /// Override this method to implement custom RPC handling.
    /// </summary>
    /// <param name="sender">The client that sent the RPC.</param>
    /// <param name="rpcId">The identifier of the RPC method.</param>
    /// <param name="packetReader">The packet reader containing RPC data.</param>
    [HideFromIl2Cpp]
    public virtual void HandleRpc(SteamNetClient sender, byte rpcId, PacketReader packetReader) { }

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
    /// Adds a child NetworkClass to this instance's collection of child network classes.
    /// This operation is only permitted before the object has been spawned in the network.
    /// </summary>
    [HideFromIl2Cpp]
    internal void AddChild(NetworkClass child)
    {
        if (IsOnNetwork) return;

        child.AmChild = true;
        child.ParentNetworkClass = this;
        ChildNetworkClasses.Add(child);
    }

    /// <summary>
    /// Spawns a new instance of a NetworkClass-derived type across the network.
    /// Creates the object locally and broadcasts spawn notification to all clients.
    /// </summary>
    /// <typeparam name="T">The type of NetworkClass to spawn, must derive from NetworkClass.</typeparam>
    /// <param name="callback">Optional callback to configure the object before spawning.</param>
    /// <param name="owner">The Steam ID of the owner who controls this network object.</param>
    /// <returns>The newly spawned NetworkClass instance.</returns>
    public static T SpawnNew<T>(Action<T> callback = default, SteamId? owner = null) where T : NetworkClass
    {
        owner ??= SteamUser.Internal.GetSteamID();

        if (PrefabIdTypeLookup.TryGetValue(typeof(T), out var prefabId))
        {
            if (NetworkPrefabs.TryGetValue(prefabId, out var prefab))
            {
                T networkClass = prefab.Clone<T>();
                networkClass.transform.SetParent(NetworkClassesObj.transform);
                callback?.Invoke(networkClass);
                NetworkDispatcher.Spawn(networkClass, owner.Value);
                networkClass.gameObject.SetActive(true);
                networkClass.gameObject.name = $"{typeof(T).Name}({networkClass.NetworkId})";
                return networkClass;
            }
        }

        return null;
    }

    /// <summary>
    /// Despawns the network object and removes it from all connected clients.
    /// Also destroys the associated game object.
    /// </summary>
    public void DespawnAndDestroy(bool despawnOnNetwork = true)
    {
        Despawn(despawnOnNetwork);
        Destroy(gameObject);
    }

    /// <summary>
    /// Despawns the network object and removes it from all connected clients.
    /// Cleans up network resources and sends despawn notification to other clients.
    /// </summary>
    public void Despawn(bool despawnOnNetwork = true)
    {
        if (AmChild && despawnOnNetwork) return;
        if (!AmOwner) return;

        if (IsOnNetwork)
        {
            NetLobby.LobbyData.NetworkClassSpawned.Remove(NetworkId);
            IsOnNetwork = false;
            OnDespawn();

            if (!AmChild)
            {
                foreach (var netChild in ChildNetworkClasses)
                {
                    netChild.Despawn(false);
                }

                NetLobby.LobbyData.NetworkIdPoolHost.ReleaseId(NetworkId);
                NetLobby.LobbyData.NetworkIdPoolNonHost.ReleaseId(NetworkId);

                if (despawnOnNetwork)
                {
                    var packet = PacketWriter.Get();
                    packet.WriteUInt(NetworkId);
                    NetworkDispatcher.SendPacket(packet, false, PacketTag.NetworkClassDespawn, PacketChannel.Main);
                    packet.Recycle();
                }
            }

            OwnerId = default;
            NetworkId = 0;
        }
    }

    /// <summary>
    /// Initializes and registers network prefabs used for object spawning across the network.
    /// This method sets up predefined prefab templates that can be instantiated and synchronized
    /// between clients during multiplayer sessions.
    /// </summary>
    internal static void SetupPrefabs()
    {
        NetworkPrefabsObj = new GameObject($"NetworkPrefabs");
        DontDestroyOnLoad(NetworkPrefabsObj);

        CreateNetworkPrefab<PlantNetworked>(1);
        CreateNetworkPrefab<ZombieNetworked>(2);
    }

    /// <summary>
    /// Creates and registers a network prefab of the specified type with a unique identifier.
    /// The prefab is marked as hidden and persistent, serving as a template for network instantiation.
    /// </summary>
    private static T CreateNetworkPrefab<T>(byte prefabId, Action<T> callback = null) where T : NetworkClass
    {
        var networkClass = RuntimePrefab.CreatePrefab<T>($"{typeof(T)}:{prefabId}");
        callback?.Invoke(networkClass);
        networkClass.PrefabId = prefabId;
        NetworkPrefabs[prefabId] = networkClass;
        PrefabIdTypeLookup[typeof(T)] = prefabId;
        return networkClass;
    }
}
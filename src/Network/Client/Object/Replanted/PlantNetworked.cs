using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Modules;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Monos;
using ReplantedOnline.Network.Client.Object.Replanted.Components;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;
using ReplantedOnline.Patches.Gameplay.Versus.Plants;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Network.Client.Object.Replanted;

/// <summary>
/// Represents a networked plant on the board, handling synchronization of plant state
/// across connected clients including plant type, position, and imitater type.
/// </summary>
internal sealed class PlantNetworked : NetworkObject
{
    private enum PlantRpcs : byte
    {
        Die,
        Shoveled,
        SquishPlant,
        Fire,
        SetZombieTarget,
        SetState
    }

    /// <summary>
    /// Gets the current target zombie.
    /// </summary>
    [HideFromIl2Cpp]
    internal Zombie Target { get; set; }

    /// <summary>
    /// The underlying plant instance that this networked object represents.
    /// </summary>
    internal Plant _Plant;

    /// <summary>
    /// The type of seed used to plant this plant when spawning.
    /// </summary>
    internal SeedType SeedType;

    /// <summary>
    /// The imitater type if this plant was created by an Imitater seed when spawning.
    /// </summary>
    internal SeedType ImitaterType;

    /// <summary>
    /// The grid X coordinate where this plant is located when spawning.
    /// </summary>
    internal int GridX;

    /// <summary>
    /// The grid Y coordinate where this plant is located when spawning.
    /// </summary>
    internal int GridY;

    internal PlantNetworkComponent LogicComponent;

    public override string GetObjectName()
    {
        return $"{Enum.GetName(_Plant.mSeedType)}Plant ({NetworkId})";
    }

    [HideFromIl2Cpp]
    protected override void OnClone(RuntimePrefab prefab)
    {
        var networkedDebugger = gameObject.AddComponent<NetworkedDebugger>();
        networkedDebugger.Initialize(this);
    }

    public override void OnInit()
    {
        LogicComponent = PlantNetworkComponent.AddComponent(this, SeedType);
        _Plant.AddNetworkedLookup(this);
    }

    private void OnDestroy()
    {
        this.RemoveNetworkedLookup();

        if (_Plant != null)
        {
            if (!Dead && !_Plant.mDead)
            {
                _Plant.DieOriginal();
            }
        }
    }

    internal bool Dead;
    private void Update()
    {
        if (!IsOnNetwork) return;

        if (_Plant == null) return;

        if (_Plant.mDead && !Dead)
        {
            _Plant.RemoveNetworkedLookup();
            _Plant = null;
            return;
        }

        LogicComponent.Update();
    }

    internal void SendDieRpc()
    {
        if (!Dead)
        {
            Dead = true;
            SendNetworkObjectRpc(PlantRpcs.Die, _Plant.mDoSpecialCountdown);
            DespawnAndDestroy();
        }
    }

    [RpcHandler(PlantRpcs.Die)]
    internal void HandleDieRpc()
    {
        Dead = true;
        _Plant?.DieOriginal();
    }

    internal void SendShoveledRpc()
    {
        if (!Dead)
        {
            Dead = true;
            _Plant?.DieOriginal();
            SendNetworkObjectRpc(PlantRpcs.Shoveled);
            DespawnAndDestroy();
        }
    }

    [RpcHandler(PlantRpcs.Shoveled)]
    internal void HandleShoveledRpc()
    {
        Dead = true;
        _Plant?.DieOriginal();
    }

    internal void SendSquashPlantRpc()
    {
        SendNetworkObjectRpc(PlantRpcs.SquishPlant);
    }

    [RpcHandler(PlantRpcs.SquishPlant)]
    internal void HandleSquashPlantRpc()
    {
        _Plant.SquishOriginal();
    }

    internal void SendFireRpc(Zombie theTargetZombie, int theRow, PlantWeapon thePlantWeapon)
    {
        SendNetworkObjectRpc(PlantRpcs.Fire, theTargetZombie, theRow, thePlantWeapon);
    }

    [RpcHandler(PlantRpcs.Fire)]
    internal void HandleFireRpc(Zombie theTargetZombie, int theRow, PlantWeapon thePlantWeapon)
    {
        _Plant.FireOriginal(theTargetZombie, theRow, thePlantWeapon);
    }

    internal void SendSetZombieTargetRpc(Zombie target)
    {
        SendNetworkObjectRpc(PlantRpcs.SetZombieTarget, target);
    }

    [RpcHandler(PlantRpcs.SetZombieTarget)]
    internal void HandleSetZombieTargetRpc(Zombie target)
    {
        Target = target;
    }

    internal void SendSetStateRpc(string state)
    {
        SendNetworkObjectRpc(PlantRpcs.SetState, state);
    }

    [RpcHandler(PlantRpcs.SetState)]
    internal void HandleSetStateRpc(string state)
    {
        if (state == NetStates.NULL_STATE)
        {
            State = null;
            return;
        }

        State = state;
    }

    /// <summary>
    /// Serializes the plant state for network transmission.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write data to</param>
    /// <param name="init">Whether this is initial synchronization data</param>
    [HideFromIl2Cpp]
    public override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init)
        {
            // Set spawn info
            packetWriter.WriteInt(GridX);
            packetWriter.WriteInt(GridY);
            packetWriter.WriteInt((int)SeedType);
            packetWriter.WriteInt((int)ImitaterType);

            LogicComponent.Serialize(packetWriter, init);

            return;
        }

        LogicComponent.Serialize(packetWriter, init);

        ClearDirtyBits();
    }

    /// <summary>
    /// Deserializes the plant state from network data and spawns the plant instance.
    /// </summary>
    /// <param name="packetReader">The packet reader to read data from</param>
    /// <param name="init">Whether this is initial synchronization data</param>
    [HideFromIl2Cpp]
    public override void Deserialize(PacketReader packetReader, bool init)
    {
        if (init)
        {
            // Read spawn info
            GridX = packetReader.ReadInt();
            GridY = packetReader.ReadInt();
            SeedType = (SeedType)packetReader.ReadInt();
            ImitaterType = (SeedType)packetReader.ReadInt();

            _Plant = SeedPacketDefinitions.SpawnPlant(SeedType, ImitaterType, GridX, GridY, false);

            OnInit();

            LogicComponent.Deserialize(packetReader, init);

            return;
        }

        LogicComponent.Deserialize(packetReader, init);
    }
}
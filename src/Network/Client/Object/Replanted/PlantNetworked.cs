using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Monos;
using ReplantedOnline.Network.Client.Object.Replanted.Components;
using ReplantedOnline.Network.Client.Object.Replanted.PlantComponents;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;
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
        ReadyToFire,
        Fire,
        SetZombieTarget
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

    private bool _waitingToDespawn;
    internal void DespawnAndDestroyWhenDeadOrNull(bool waitToBeReady = false)
    {
        if (!_waitingToDespawn)
        {
            _waitingToDespawn = true;
            this.StartCoroutine(CoroutineUtils.WaitForCondition(() => _Plant == null || (_Plant.mDead && Dead), () =>
            {
                DespawnAndDestroy(waitToBeReady);
            }));
        }
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
            DespawnAndDestroy(true);
        }
    }

    [RpcHandler(PlantRpcs.Die)]
    private void HandleDieRpc()
    {
        Dead = true;
        _Plant?.DieOriginal();
        IsReadyToDespawn = true;
    }

    internal void SendShoveledRpc()
    {
        if (!Dead)
        {
            Dead = true;
            _Plant?.DieOriginal();
            SendNetworkObjectRpc(PlantRpcs.Shoveled);
            DespawnAndDestroy(true);
        }
    }

    [RpcHandler(PlantRpcs.Shoveled)]
    private void HandleShoveledRpc()
    {
        Dead = true;
        _Plant?.DieOriginal();
        Instances.GameplayActivity.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_PLANT2);
        IsReadyToDespawn = true;
    }

    internal void SendSquashPlantRpc()
    {
        SendNetworkObjectRpc(PlantRpcs.SquishPlant);
    }

    [RpcHandler(PlantRpcs.SquishPlant)]
    private void HandleSquashPlantRpc()
    {
        _Plant.SquishOriginal();
    }

    internal void SendReadyToFireRpc(int row, ref PlantWeapon plantWeapon)
    {
        if (LogicComponent is KernelpultNetworkComponent component)
        {
            component.RandomizeWeapon();
            plantWeapon = component.GetWeapon();
        }

        SendNetworkObjectRpc(PlantRpcs.ReadyToFire, row, plantWeapon);
    }

    [RpcHandler(PlantRpcs.ReadyToFire)]
    private void HandleReadyToFireRpc(int row, PlantWeapon plantWeapon)
    {
        if (LogicComponent is KernelpultNetworkComponent component)
        {
            component.SetVisuals(plantWeapon);
        }

        // Play animation
        _Plant.FindTargetAndFireOriginal(row, plantWeapon);
    }

    internal void SendFireRpc(Zombie target, int row, ref PlantWeapon plantWeapon)
    {
        if (LogicComponent is KernelpultNetworkComponent component)
        {
            component.SetVisuals(PlantWeapon.Primary);
            plantWeapon = component.GetWeapon();
        }

        SendNetworkObjectRpc(PlantRpcs.Fire, target, row, plantWeapon);
    }

    [RpcHandler(PlantRpcs.Fire)]
    private void HandleFireRpc(Zombie target, int row, PlantWeapon plantWeapon)
    {
        if (LogicComponent is KernelpultNetworkComponent component)
        {
            component.SetVisuals(PlantWeapon.Primary);
        }

        _Plant.FireOriginal(target, row, plantWeapon);
    }

    internal void SendSetZombieTargetRpc(Zombie target)
    {
        SendNetworkObjectRpc(PlantRpcs.SetZombieTarget, target);
    }

    [RpcHandler(PlantRpcs.SetZombieTarget)]
    private void HandleSetZombieTargetRpc(Zombie target)
    {
        Target = target;
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
            packetWriter.WriteEnum(SeedType);

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
            SeedType = packetReader.ReadEnum<SeedType>();

            _Plant = SeedPacketDefinitions.SpawnPlant(SeedType, GridX, GridY, false).Plant;

            OnInit();

            LogicComponent.Deserialize(packetReader, init);

            return;
        }

        LogicComponent.Deserialize(packetReader, init);
    }
}
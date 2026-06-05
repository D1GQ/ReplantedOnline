using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using MelonLoader;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Modded;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.MonoScripts.Network;
using ReplantedOnline.MonoScripts.Unity;
using ReplantedOnline.Network.Client.Object.Reloaded.Components;
using ReplantedOnline.Network.Client.Object.Reloaded.PlantComponents;
using ReplantedOnline.Network.Routing.Packet;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Networked;
using ReplantedOnline.Utilities.Modded;
using ReplantedOnline.Utilities.Unity;

namespace ReplantedOnline.Network.Client.Object.Reloaded;

/// <summary>
/// Represents a networked plant on the board, handling synchronization of plant state
/// </summary>
[RegisterTypeInIl2Cpp]
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
    internal Zombie? Target { get; set; }

    internal WeakVar<Plant> _p = new();

    /// <summary>
    /// The underlying plant instance that this networked object represents.
    /// </summary>
    internal Plant? Plant => _p.Value;

    /// <summary>
    /// The type of seed used to plant this plant when spawning.
    /// </summary>
    internal SeedType SeedType;

    /// <summary>
    /// The spawn type of the plant.
    /// </summary>
    internal SpawnType SpawnType;

    /// <summary>
    /// The grid X coordinate where this plant is located when spawning.
    /// </summary>
    internal int GridX;

    /// <summary>
    /// The grid Y coordinate where this plant is located when spawning.
    /// </summary>
    internal int GridY;

    internal PlantNetworkComponent LogicComponent = default!;

    public override string GetObjectName()
    {
        return $"{Enum.GetName(Plant!.mSeedType)}Plant ({NetworkId})";
    }

    [HideFromIl2Cpp]
    protected override void OnClone(RuntimePrefab prefab)
    {
        var networkedDebugger = gameObject.AddComponent<NetworkedDebugger>();
        networkedDebugger.Initialize(this);
    }

    public override void OnInit()
    {
        LogicComponent = (PlantNetworkComponent)RegisterNetworkComponent.TryCreateInstance(SeedType, typeof(PlantNetworkComponent))!;
        AddNetworkComponent(LogicComponent);
        Plant?.AddNetworkedLookup(this);

        if (SpawnType == SpawnType.ChinaJalapeno)
        {
            LogicComponent = AddNewNetworkComponent<ChinaJalapenoNetworkComponent>();
        }
    }

    private bool _waitingToDespawn;
    internal void DespawnAndDestroyWhenDeadOrNull(bool waitToBeReady = false)
    {
        if (!_waitingToDespawn)
        {
            _waitingToDespawn = true;
            this.StartCoroutine(CoroutineUtils.WaitForCondition(() => Plant == null || Plant.mDead && Dead, () =>
            {
                DespawnAndDestroy(waitToBeReady);
            }));
        }
    }

    public override void OnRejected()
    {
        if (Plant == null) return;

        if (!Plant.mDead)
        {
            Plant?.DieOriginal();
        }
    }

    public override void OnDestroyed()
    {
        this.RemoveNetworkedLookup();

        if (Plant != null)
        {
            if (!Dead && !Plant.mDead)
            {
                Plant.DieOriginal();
            }
        }
    }

    internal bool Dead;
    private void Update()
    {
        if (!IsOnNetwork) return;

        if (Plant == null) return;

        LogicComponent.Update();
    }

    internal void SendDieRpc()
    {
        if (!Dead)
        {
            Dead = true;
            SendNetworkObjectRpc(PlantRpcs.Die, Plant?.mDoSpecialCountdown ?? 0);
            DespawnAndDestroy(true);
        }
    }

    [RpcHandler(PlantRpcs.Die)]
    private void HandleDieRpc()
    {
        if (!Dead)
        {
            Dead = true;
            Plant?.DieOriginal();
        }
        IsReadyToDespawn = true;
    }

    internal void SendShoveledRpc()
    {
        if (!Dead)
        {
            Dead = true;
            Plant?.DieOriginal();
            SendNetworkObjectRpc(PlantRpcs.Shoveled);
            DespawnAndDestroy(true);
        }
    }

    [RpcHandler(PlantRpcs.Shoveled)]
    private void HandleShoveledRpc()
    {
        if (!Dead)
        {
            Dead = true;
            Plant?.DieOriginal();
            Instances.GameplayActivity.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_PLANT2);
        }

        IsReadyToDespawn = true;
    }

    internal void SendSquashPlantRpc()
    {
        SendNetworkObjectRpc(PlantRpcs.SquishPlant);
    }

    [RpcHandler(PlantRpcs.SquishPlant)]
    private void HandleSquashPlantRpc()
    {
        Plant?.SquishOriginal();
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
        Plant?.FindTargetAndFireOriginal(row, plantWeapon);
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

        Plant?.FireOriginal(target, row, plantWeapon);
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
            packetWriter.WriteEnum(SeedType);
            packetWriter.WriteEnum(SpawnType);
            packetWriter.WriteInt(GridX);
            packetWriter.WriteInt(GridY);

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
            SeedType = packetReader.ReadEnum<SeedType>();
            SpawnType = packetReader.ReadEnum<SpawnType>();
            GridX = packetReader.ReadInt();
            GridY = packetReader.ReadInt();

            var plant = SeedPacketDefinitions.SpawnPlant(SeedType, GridX, GridY, false).Plant;
            _p.SetTarget(() => plant?.mController?.m_plant);

            OnInit();

            LogicComponent.Deserialize(packetReader, init);

            return;
        }

        LogicComponent.Deserialize(packetReader, init);
    }
}
using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using MelonLoader;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Managers.Reloaded;
using ReplantedOnline.Modules.Modded;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.MonoScripts.Network;
using ReplantedOnline.MonoScripts.Unity;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.PlantComponents;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Sync;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Structs.Reloaded;
using ReplantedOnline.Utilities.Modded;
using ReplantedOnline.Utilities.Unity;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay;

/// <summary>
/// Represents a networked plant on the board, handling synchronization of plant state
/// </summary>
[RegisterTypeInIl2Cpp]
internal sealed class PlantNetworked : NetworkObject
{
    private enum PlantMsgs : byte
    {
        Die,
        Shoveled,
        SquishPlant,
        ReadyToFire,
        Fire,
        SetZombieTarget,
        Upgrade
    }

    /// <summary>
    /// Gets the current target zombie.
    /// </summary>
    [HideFromIl2Cpp]
    internal Zombie? Target { get; set; }

    internal readonly LazyReference<Plant> _p = new();

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
    internal BoardUnitX BoardUnitX;

    /// <summary>
    /// The grid Y coordinate where this plant is located when spawning.
    /// </summary>
    internal BoardUnitY BoardUnitY;

    /// <summary>
    /// Gets or sets if the the plant is currently dying on the network.
    /// </summary>
    internal bool Dying { get; set; }

    internal PlantNetworkComponent LogicComponent = default!;

    public sealed override string GetObjectName()
    {
        return $"{Enum.GetName(Plant!.mSeedType)}Plant ({NetworkId})";
    }

    [HideFromIl2Cpp]
    protected sealed override void OnClone(RuntimePrefab prefab)
    {
        var networkedDebugger = gameObject.AddComponent<NetworkedDebugger>();
        networkedDebugger.Initialize(this);
    }

    public sealed override void OnInit()
    {
        LogicComponent = (PlantNetworkComponent)RegisterNetworkComponent.TryCreateInstance(SeedType, typeof(PlantNetworkComponent))!;
        AddNetworkComponent(LogicComponent);
        var plant = Plant;
        if (plant != null)
        {
            plant.AddNetworkedLookup(this);

            if (plant.mSubclass == PlantSubClass.Shooter)
            {
                plant.mLaunchRate += VersusGameplayManager.GetVersusModeConfig().PlantShooterLaunchRateSurplus;
            }
        }

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
            this.StartCoroutine(CoroutineUtils.WaitForCondition(() => Plant == null || Plant.mDead, () =>
            {
                DespawnAndDestroy(waitToBeReady);
            }));
        }
    }

    public sealed override void OnRejected()
    {
        var plant = Plant;
        if (plant == null)
            return;

        if (!plant.mDead)
        {
            plant.DieOriginal();
        }
    }

    public sealed override void OnDestroyed()
    {
        this.RemoveNetworkedLookup();

        var plant = Plant;
        if (plant == null)
            return;

        if (!Dying)
        {
            plant.DieOriginal();
        }
    }

    private void Update()
    {
        if (!IsOnNetwork)
            return;

        LogicComponent.Update();
    }

    internal void SendDieMessage()
    {
        if (!Dying)
        {
            Dying = true;
            SendMessage(PlantMsgs.Die);
        }

        DespawnAndDestroy(true);
    }

    [ObjectMessageHandler(PlantMsgs.Die)]
    private void HandleDieMessage()
    {
        if (!Dying)
        {
            Dying = true;
            Plant?.DieOriginal();
        }

        IsReadyToDespawn = true;
    }

    internal void SendShoveledMessage()
    {
        if (!Dying)
        {
            Dying = true;
            Plant?.DieOriginal();
            SendMessage(PlantMsgs.Shoveled);
        }

        DespawnAndDestroy(true);
    }

    [ObjectMessageHandler(PlantMsgs.Shoveled)]
    private void HandleShoveledMessage()
    {
        if (!Dying)
        {
            Dying = true;
            Plant?.DieOriginal();
            Instances.GameplayActivity.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_PLANT2);
        }

        IsReadyToDespawn = true;
    }

    internal void SendSquashPlantMessage()
    {
        if (!Dying)
        {
            Dying = true;
            SendMessage(PlantMsgs.SquishPlant);
        }

        DespawnAndDestroyWhenDeadOrNull(true);
    }

    [ObjectMessageHandler(PlantMsgs.SquishPlant)]
    private void HandleSquashPlantMessage()
    {
        if (!Dying)
        {
            Dying = true;
            Plant?.SquishOriginal();
        }

        IsReadyToDespawn = true;
    }

    internal void SendReadyToFireMessage(PackedInt row, ref PlantWeapon plantWeapon)
    {
        if (LogicComponent is KernelpultNetworkComponent component)
        {
            component.RandomizeWeapon();
            plantWeapon = component.GetWeapon();
        }

        SendMessage(PlantMsgs.ReadyToFire, row, plantWeapon);
    }

    [ObjectMessageHandler(PlantMsgs.ReadyToFire)]
    private void HandleReadyToFireMessage(PackedInt row, PlantWeapon plantWeapon)
    {
        if (LogicComponent is KernelpultNetworkComponent component)
        {
            component.SetVisuals(plantWeapon);
        }

        // Play animation
        Plant?.FindTargetAndFireOriginal(row, plantWeapon);
    }

    internal void SendFireMessage(Zombie target, PackedInt row, ref PlantWeapon plantWeapon)
    {
        if (LogicComponent is KernelpultNetworkComponent component)
        {
            component.SetVisuals(PlantWeapon.Primary);
            plantWeapon = component.GetWeapon();
        }

        SendMessage(PlantMsgs.Fire, target, row, plantWeapon);
    }

    [ObjectMessageHandler(PlantMsgs.Fire)]
    private void HandleFireMessage(Zombie target, PackedInt row, PlantWeapon plantWeapon)
    {
        if (LogicComponent is KernelpultNetworkComponent component)
        {
            component.SetVisuals(PlantWeapon.Primary);
        }

        Plant?.FireOriginal(target, row, plantWeapon);
    }

    internal void SendSetZombieTargetMessage(Zombie target)
    {
        SendMessage(PlantMsgs.SetZombieTarget, target);
    }

    [ObjectMessageHandler(PlantMsgs.SetZombieTarget)]
    private void HandleSetZombieTargetMessage(Zombie target)
    {
        Target = target;
    }

    internal void SendUpgradeMessage(SeedType upgrade)
    {
        if (Plant == null)
            return;

        if (!Plant.IsUpgradableTo(upgrade))
            return;

        if (VersusGameplayManager.GetVersusModeConfig().GetPlantConfig(upgrade) == null)
            return;

        bool wasOnNetwork = IsOnNetwork;
        IsOnNetwork = false;

        var plant = Plant;
        plant.mController.Die();
        SeedType = upgrade;
        plant.PlantInitialize(BoardUnitX, BoardUnitY, upgrade, SeedType.None);
        plant.mBoard.DoPlantingEffects(BoardUnitX, BoardUnitY, plant);

        _p.SetTarget(() => plant.mController.m_plant);
        IsOnNetwork = wasOnNetwork;
        gameObject.name = GetObjectName();

        SendMessage(PlantMsgs.Upgrade, upgrade);
    }

    [ObjectMessageHandler(PlantMsgs.Upgrade)]
    private void HandleUpgradeMessage(SeedType upgrade)
    {
        if (Plant == null)
            return;

        if (!Plant.IsUpgradableTo(upgrade))
            return;

        if (VersusGameplayManager.GetVersusModeConfig().GetPlantConfig(upgrade) == null)
            return;

        bool wasOnNetwork = IsOnNetwork;
        IsOnNetwork = false;

        var plant = Plant;
        plant.mController.Die();
        SeedType = upgrade;
        plant.PlantInitialize(BoardUnitX, BoardUnitY, upgrade, SeedType.None);
        plant.mBoard.DoPlantingEffects(BoardUnitX, BoardUnitY, plant);

        _p.SetTarget(() => plant.mController.m_plant);
        IsOnNetwork = wasOnNetwork;
        gameObject.name = GetObjectName();
    }

    /// <summary>
    /// Serializes the plant state for network transmission.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write data to</param>
    /// <param name="init">Whether this is initial synchronization data</param>
    [HideFromIl2Cpp]
    public sealed override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init)
        {
            // Set spawn info
            packetWriter.WriteEnum(SeedType);
            packetWriter.WriteEnum(SpawnType);
            packetWriter.WriteBoardUnitX(BoardUnitX);
            packetWriter.WriteBoardUnitY(BoardUnitY);

            LogicComponent.Serialize(packetWriter, init);
            ClearDirtyBits();

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
    public sealed override void Deserialize(PacketReader packetReader, bool init)
    {
        if (init)
        {
            // Read spawn info
            SeedType = packetReader.ReadEnum<SeedType>();
            SpawnType = packetReader.ReadEnum<SpawnType>();
            BoardUnitX = packetReader.ReadBoardUnitX();
            BoardUnitY = packetReader.ReadBoardUnitY();

            var plant = SeedPacketDefinitions.SpawnPlant(SeedType, BoardUnitX, BoardUnitY, false).Plant;
            _p.SetTarget(() => plant?.mController?.m_plant);

            OnInit();

            LogicComponent.Deserialize(packetReader, init);

            return;
        }

        LogicComponent.Deserialize(packetReader, init);
    }
}
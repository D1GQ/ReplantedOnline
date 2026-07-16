using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using MelonLoader;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Managers.Reloaded;
using ReplantedOnline.Modules.Modded;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.MonoScripts.Network;
using ReplantedOnline.MonoScripts.Unity;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Networked;
using ReplantedOnline.Utilities.Modded;
using ReplantedOnline.Utilities.Unity;
using UnityEngine;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay;

/// <summary>
/// Represents a networked zombie on the board, handling synchronization of zombie state
/// </summary>
[RegisterTypeInIl2Cpp]
internal sealed class ZombieNetworked : NetworkObject
{
    private enum ZombieRpcs : byte
    {
        TakeDamage,
        Death,
        DieLoot,
        DragUnder,
        MowDown,
        SetPlantTarget,
        EnteringHouse,
        MindControlled,
        SetFrozen,
        ApplyBurn,
        SnapToPos,
        SetState,
        MoveToRow
    }

    /// <summary>
    /// Gets the current target zombie.
    /// </summary>
    [HideFromIl2Cpp]
    internal Plant? Target { get; set; }

    internal WeakVar<Zombie> _z = new();

    /// <summary>
    /// The underlying zombie instance that this networked object represents.
    /// </summary>
    internal Zombie? Zombie => _z.Value;

    /// <summary>
    /// The type of zombie this networked object represents when spawning.
    /// </summary>
    internal ZombieType ZombieType;

    /// <summary>
    /// The spawn type of the zombie.
    /// </summary>
    internal SpawnType SpawnType;

    /// <summary>
    /// The grid X coordinate where this zombie is located when spawning.
    /// </summary>
    internal int GridX;

    /// <summary>
    /// The grid Y coordinate where this zombie is located when spawning.
    /// </summary>
    internal int GridY;

    /// <summary>
    /// The current event state the zombie is in.
    /// </summary>
    internal EventState Event;

    /// <summary>
    /// Gets or sets if the the zombie is currently dying on the network.
    /// </summary>
    internal bool Dying { get; set; }

    internal ZombieCustomPoolLogicNetworkComponent PoolComponent = default!;
    internal ZombieNetworkComponent LogicComponent = default!;
    internal bool EnteringHouse;

    public sealed override string GetObjectName()
    {
        return $"{Enum.GetName(Zombie!.mZombieType)}Zombie ({NetworkId})";
    }

    [HideFromIl2Cpp]
    protected sealed override void OnClone(RuntimePrefab prefab)
    {
        var networkedDebugger = gameObject.AddComponent<NetworkedDebugger>();
        networkedDebugger.Initialize(this);
    }

    public sealed override void OnInit()
    {
        LogicComponent = (ZombieNetworkComponent)RegisterNetworkComponent.TryCreateInstance(ZombieType, typeof(ZombieNetworkComponent))!;
        AddNetworkComponent(LogicComponent);

        PoolComponent = AddNewNetworkComponent<ZombieCustomPoolLogicNetworkComponent>();

        if (SpawnType is SpawnType.BungeeDropZombie or SpawnType.BungeeDropZombieNoTarget)
        {
            LogicComponent = AddNewNetworkComponent<BungeeDropZombieComponent>();
        }

        Zombie?.AddNetworkedLookup(this);
    }

    private bool _waitingToDespawn;
    internal void DespawnAndDestroyWhenNullOrDead(bool waitToBeReady = false)
    {
        if (!_waitingToDespawn)
        {
            _waitingToDespawn = true;
            this.StartCoroutine(CoroutineUtils.WaitForCondition(() => Zombie == null || Zombie.mDead, () =>
            {
                DespawnAndDestroy(waitToBeReady);
            }));
        }
    }

    public sealed override void OnRejected()
    {
        if (Zombie == null) return;

        if (!Zombie.mDead)
        {
            Zombie.DieNoLootOriginal();
        }
    }

    public sealed override void OnDestroyed()
    {
        this.RemoveNetworkedLookup();

        if (Zombie != null && !Dying)
        {
            Zombie.DieNoLootOriginal();
        }
    }

    private void Update()
    {
        if (!IsOnNetwork) return;

        if (Zombie == null) return;

        PoolComponent.Update();
        LogicComponent.Update();
    }

    // For some reason TakeDamage gets triggered twice naturally, so we must not send the rpc on the second time using damageInterval.
    private readonly ExecuteInterval damageInterval = new();
    internal void SendTakeDamageRpc(int theDamage, DamageFlags theDamageFlags)
    {
        if (damageInterval.Execute())
        {
            SendNetworkObjectRpc(ZombieRpcs.TakeDamage, theDamage, theDamageFlags);
        }
    }

    [RpcHandler(ZombieRpcs.TakeDamage)]
    internal void HandleTakeDamageRpc(int theDamage, DamageFlags damageFlags)
    {
        if (Zombie == null) return;

        int minimumHealth = 5;
        int relevantHealth;

        if (((damageFlags & DamageFlags.Spike) == DamageFlags.Spike ||
            (damageFlags & DamageFlags.BypassesShield) == DamageFlags.BypassesShield) &&
            ZombieType != ZombieType.Target)
        {
            relevantHealth = Zombie.mBodyHealth + Zombie.mHelmHealth;
        }
        else
        {
            relevantHealth = Zombie.mBodyHealth + Zombie.mHelmHealth + Zombie.mShieldHealth;
        }

        if (theDamage >= relevantHealth - (minimumHealth - 1))
        {
            int damageToApply = relevantHealth - minimumHealth;

            if (damageToApply > 0)
            {
                Zombie.TakeDamageOriginal(damageToApply, damageFlags);
            }
        }
        else
        {
            Zombie.TakeDamageOriginal(theDamage, damageFlags);
        }
    }

    internal void SendDeathRpc(DamageFlags damageFlags)
    {
        if (!Dying)
        {
            Dying = true;
            LogicComponent.OnDeath(DeathReason.Normal);
            SendNetworkObjectRpc(ZombieRpcs.Death, damageFlags);
        }

        DespawnAndDestroy(true);
    }

    [RpcHandler(ZombieRpcs.Death)]
    private void HandleDeathRpc(DamageFlags damageFlags)
    {
        if (!Dying)
        {
            Dying = true;
            LogicComponent.OnDeath(DeathReason.Normal);
            Zombie?.PlayDeathAnimOriginal(damageFlags);
        }

        IsReadyToDespawn = true;
    }

    internal void SendDieLootRpc(bool withLoot)
    {
        if (!Dying)
        {
            Dying = true;
            LogicComponent.OnDeath(DeathReason.Normal);
            SendNetworkObjectRpc(ZombieRpcs.DieLoot, withLoot);
        }

        DespawnAndDestroy(true);
    }

    [RpcHandler(ZombieRpcs.DieLoot)]
    private void HandleDieLootRpc(bool withLoot)
    {
        if (!Dying)
        {
            Dying = true;
            LogicComponent.OnDeath(DeathReason.Normal);
            if (withLoot)
            {
                Zombie?.DieWithLootOriginal();
            }
            else
            {
                Zombie?.DieNoLootOriginal();
            }
        }

        IsReadyToDespawn = true;
    }

    internal void SendDragUnderRpc()
    {
        SendNetworkObjectRpc(ZombieRpcs.DragUnder);
    }

    [RpcHandler(ZombieRpcs.DragUnder)]
    private void HandleDragUnderRpc()
    {
        Zombie?.DragUnderOriginal();
    }

    internal void SendMowDownRpc()
    {
        if (!Dying)
        {
            Dying = true;
            LogicComponent.OnDeath(DeathReason.Normal);
            SendNetworkObjectRpc(ZombieRpcs.MowDown);
        }

        DespawnAndDestroy(true);
    }

    [RpcHandler(ZombieRpcs.MowDown)]
    private void HandleMowDownRpc()
    {
        if (!Dying)
        {
            Dying = true;
            LogicComponent.OnDeath(DeathReason.Normal);
            Zombie?.MowDownOriginal();
        }

        IsReadyToDespawn = true;
    }

    internal void SendSetPlantTargetRpc(Plant target)
    {
        if (Target != target)
        {
            Target = target;
            SendNetworkObjectRpc(ZombieRpcs.SetPlantTarget, target);
        }
    }

    [RpcHandler(ZombieRpcs.SetPlantTarget)]
    private void HandleSetPlantTargetRpc(Plant target)
    {
        Target = target;
    }

    internal void SendEnteringHouseRpc(float xPos)
    {
        EnteringHouse = true;
        SendNetworkObjectRpc(ZombieRpcs.EnteringHouse, xPos);
    }

    [RpcHandler(ZombieRpcs.EnteringHouse)]
    private void HandleEnteringHouseRpc(float xPos)
    {
        EnteringHouse = true;
        VersusGameplayManager.EndGame(Zombie?.mController?.transform.position ?? Vector3.zero, PlayerTeam.Zombies);
        if (Zombie != null)
        {
            Zombie.mPosX = xPos;
            Zombie.mBoard.mCutScene.StartZombiesWon();
        }
    }

    internal void SendMindControlledRpc()
    {
        State = ReplantedOnlineMod.Constants.Network.ObjectStates.ZOMBIE_MIND_CONTROLLED_STATE;
        SendNetworkObjectRpc(ZombieRpcs.MindControlled);
        DespawnAndDestroyWhenNullOrDead();
    }

    [RpcHandler(ZombieRpcs.MindControlled)]
    private void HandleMindControlledRpc()
    {
        State = ReplantedOnlineMod.Constants.Network.ObjectStates.ZOMBIE_MIND_CONTROLLED_STATE;
        Zombie?.StartMindControlledOriginal();
    }

    internal void SendSetFrozenRpc(bool frozen)
    {
        this.StartCoroutine(CoroutineUtils.WaitForCondition(() => !frozen || Zombie?.mChilledCounter > 0, () =>
        {
            int counter;
            if (Zombie == null)
            {
                counter = 0;
            }
            else if (frozen)
            {
                counter = Zombie.mIceTrapCounter;
            }
            else
            {
                counter = Zombie.mChilledCounter;
            }

            SendNetworkObjectRpc(ZombieRpcs.SetFrozen, frozen, counter);
        }));
    }

    [RpcHandler(ZombieRpcs.SetFrozen)]
    private void HandleSetFrozenRpc(bool frozen, int counter)
    {
        if (Zombie == null)
            return;

        if (frozen)
        {
            Zombie.HitIceTrapOriginal();
            Zombie.mIceTrapCounter = counter;
        }
        else
        {
            Zombie.RemoveIceTrapOriginal();
            Zombie.mChilledCounter = counter;
        }
    }

    internal void SendApplyBurnRpc()
    {
        if (Zombie == null)
            return;

        bool reallyDead = Zombie.mBodyHealth <= 1800;

        SendNetworkObjectRpc(ZombieRpcs.ApplyBurn, reallyDead);

        if (reallyDead && !Dying)
        {
            Dying = true;
            LogicComponent.OnDeath(DeathReason.Burned);
            DespawnAndDestroy(true);
        }
    }

    [RpcHandler(ZombieRpcs.ApplyBurn)]
    private void HandleApplyBurnRpc(bool reallyDead)
    {
        if (Zombie == null)
            return;

        if (Zombie.mZombieType.IsGravestoneOrTarget())
            return;

        if (reallyDead && !Dying)
        {
            Dying = true;
            LogicComponent.OnDeath(DeathReason.Burned);
            Zombie?.ApplyBurnOriginal();
            IsReadyToDespawn = true;
        }
    }

    internal void SendSnapToPosRpc()
    {
        SendNetworkObjectRpc(ZombieRpcs.SnapToPos, Zombie?.mPosX ?? 0);
    }

    [RpcHandler(ZombieRpcs.SnapToPos)]
    private void HandleSnapToPosRpc(float posX)
    {
        Zombie?.mPosX = posX;
    }

    internal void SendSetStateRpc(string state)
    {
        SendNetworkObjectRpc(ZombieRpcs.SetState, state);
    }

    [RpcHandler(ZombieRpcs.SetState)]
    private void HandleSetStateRpc(string state)
    {
        if (state == ReplantedOnlineMod.Constants.Network.ObjectStates.NULL_STATE)
        {
            State = null;
            return;
        }

        State = state;
    }

    internal void SendMoveToRowRpc(int row)
    {
        SendNetworkObjectRpc(ZombieRpcs.MoveToRow, row);
    }

    [RpcHandler(ZombieRpcs.MoveToRow)]
    private void HandleSnapToPosRpc(int row)
    {
        if (Zombie == null)
            return;

        Zombie.mRow = row;
        Zombie.mRenderOrder = Board.MakeRenderOrder(RenderLayer.Zombie, row, 4);
        PoolComponent.WhiteWaterEffect?.UpdateSortingOrder(Zombie.mController);
    }

    [HideFromIl2Cpp]
    public sealed override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init)
        {
            // Set spawn info
            packetWriter.WriteEnum(ZombieType);
            packetWriter.WriteEnum(SpawnType);
            packetWriter.WriteInt(GridX);
            packetWriter.WriteInt(GridY);

            LogicComponent.Serialize(packetWriter, init);
            ClearDirtyBits();

            return;
        }

        LogicComponent.Serialize(packetWriter, init);
        ClearDirtyBits();
    }

    [HideFromIl2Cpp]
    public sealed override void Deserialize(PacketReader packetReader, bool init)
    {
        if (init)
        {
            // Read spawn info
            ZombieType = packetReader.ReadEnum<ZombieType>();
            SpawnType = packetReader.ReadEnum<SpawnType>();
            GridX = packetReader.ReadInt();
            GridY = packetReader.ReadInt();

            var zombie = SeedPacketDefinitions.SpawnZombie(ZombieType, GridX, GridY, SpawnType, false).Zombie;
            _z.SetTarget(() => zombie?.mController?.m_zombie);

            OnInit();

            LogicComponent.Deserialize(packetReader, init);

            return;
        }

        LogicComponent.Deserialize(packetReader, init);
    }
}
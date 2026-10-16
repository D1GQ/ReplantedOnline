using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using MelonLoader;
using ReplantedOnline.Attributes.Network;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Managers.Reloaded;
using ReplantedOnline.Modules.Modded;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.MonoScripts.Network;
using ReplantedOnline.MonoScripts.Unity;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;
using ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.ZombieComponents;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Sync;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Structs.Reloaded;
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
    /// <summary>
    /// Gets the current target zombie.
    /// </summary>
    [HideFromIl2Cpp]
    internal Plant? Target { get; set; }

    internal readonly LazyReference<Zombie> _z = new();

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
    internal BoardUnitX BoardUnitX;

    /// <summary>
    /// The grid Y coordinate where this zombie is located when spawning.
    /// </summary>
    internal BoardUnitY BoardUnitY;

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
        var zombie = Zombie;
        if (zombie == null)
            return;

        if (!zombie.mDead)
        {
            zombie.DieNoLootOriginal();
        }
    }

    public sealed override void OnDestroyed()
    {
        this.RemoveNetworkedLookup();

        var zombie = Zombie;
        if (zombie == null)
            return;

        if (!Dying)
        {
            zombie.DieNoLootOriginal();
        }
    }

    private void Update()
    {
        if (!IsOnNetwork)
            return;

        PoolComponent.Update();
        LogicComponent.Update();
    }

    [MethodRpc(LocalExecution.None)]
    internal void RpcTakeDamage(PackedInt theDamage, DamageFlags theDamageFlags)
    {
        if (Zombie == null)
            return;

        int minimumHealth = 5;
        int relevantHealth;

        if (((theDamageFlags & DamageFlags.Spike) == DamageFlags.Spike ||
            (theDamageFlags & DamageFlags.BypassesShield) == DamageFlags.BypassesShield) &&
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
                Zombie.TakeDamageOriginal(damageToApply, theDamageFlags);
            }
        }
        else
        {
            Zombie.TakeDamageOriginal(theDamage, theDamageFlags);
        }
    }

    [HideFromIl2Cpp]
    [MethodRpc(LocalExecution.Before)]
    internal void RpcDeath(DamageFlags damageFlags, MethodRpc.RpcInfo rpcInfo = default!)
    {
        if (Dying)
        {
            rpcInfo.Local.CancelSend = true;
            return;
        }

        Dying = true;

        LogicComponent.OnDeath(Zombie, DeathReason.Normal);

        if (rpcInfo.IsLocal)
        {
            DespawnAndDestroyWhenNullOrDead(true);
        }
        else
        {
            Zombie?.PlayDeathAnimOriginal(damageFlags);
            IsReadyToDespawn = true;
        }
    }

    [HideFromIl2Cpp]
    [MethodRpc(LocalExecution.Before)]
    internal void RpcDieLoot(bool withLoot, MethodRpc.RpcInfo rpcInfo = default!)
    {
        if (Dying)
        {
            rpcInfo.Local.CancelSend = true;
            return;
        }

        Dying = true;

        LogicComponent.OnDeath(Zombie, DeathReason.Normal);

        if (rpcInfo.IsLocal)
        {
            DespawnAndDestroy(true);
        }
        else
        {
            if (withLoot)
            {
                Zombie?.DieWithLootOriginal();
            }
            else
            {
                Zombie?.DieNoLootOriginal();
            }
            IsReadyToDespawn = true;
        }
    }

    [MethodRpc(LocalExecution.None)]
    internal void RpcDragUnder()
    {
        Zombie?.DragUnderOriginal();
    }

    [HideFromIl2Cpp]
    [MethodRpc(LocalExecution.Before)]
    internal void RpcMowDown(MethodRpc.RpcInfo rpcInfo = default!)
    {
        if (Dying)
        {
            rpcInfo.Local.CancelSend = true;
            return;
        }

        Dying = true;

        LogicComponent.OnDeath(Zombie, DeathReason.Normal);

        if (rpcInfo.IsLocal)
        {
            DespawnAndDestroy(true);
        }
        else
        {
            Zombie?.MowDownOriginal();
            IsReadyToDespawn = true;
        }
    }

    [HideFromIl2Cpp]
    [MethodRpc(LocalExecution.Before)]
    internal void RpcSetPlantTarget(Plant target, MethodRpc.RpcInfo rpcInfo = default!)
    {
        if (rpcInfo.IsLocal && Target == target)
        {
            rpcInfo.Local.CancelSend = true;
            return;
        }

        Target = target;
    }

    [HideFromIl2Cpp]
    [MethodRpc(LocalExecution.Before)]
    internal void RpcEnteringHouse(float xPos, MethodRpc.RpcInfo rpcInfo = default!)
    {
        if (EnteringHouse)
        {
            rpcInfo.Local.CancelSend = true;
            return;
        }

        EnteringHouse = true;

        VersusGameplayManager.EndGame(Zombie?.mController?.transform.position ?? Vector3.zero, PlayerTeam.Zombies);
        if (Zombie != null)
        {
            Zombie.mPosX = xPos;
            Zombie.mBoard.mCutScene.StartZombiesWon();
        }
    }

    [HideFromIl2Cpp]
    [MethodRpc(LocalExecution.Before)]
    internal void RpcMindControlled(MethodRpc.RpcInfo rpcInfo = default!)
    {
        if (State is ReplantedOnlineMod.Constants.Network.ObjectStates.ZOMBIE_MIND_CONTROLLED_STATE)
        {
            rpcInfo.Local.CancelSend = true;
            return;
        }

        State = ReplantedOnlineMod.Constants.Network.ObjectStates.ZOMBIE_MIND_CONTROLLED_STATE;

        if (rpcInfo.IsLocal)
        {
            DespawnAndDestroyWhenNullOrDead();
        }
        else
        {
            Zombie?.StartMindControlledOriginal();
        }
    }

    [HideFromIl2Cpp]
    [MethodRpc(LocalExecution.Before)]
    internal void RpcSetFrozen(bool hitIceTrap, PackedInt counter = default, MethodRpc.RpcInfo rpcInfo = default!)
    {
        if (rpcInfo.IsLocal)
        {
            if (Zombie == null)
            {
                rpcInfo.Local.Args[1] = (PackedInt)0;
            }
            else if (hitIceTrap)
            {
                rpcInfo.Local.Args[1] = (PackedInt)Zombie.mIceTrapCounter;
            }
            else
            {
                rpcInfo.Local.Args[1] = (PackedInt)Zombie.mChilledCounter;
            }
        }
        else
        {
            if (Zombie == null)
                return;

            if (hitIceTrap)
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
    }

    [HideFromIl2Cpp]
    [MethodRpc(LocalExecution.Before)]
    internal void RpcApplyBurn(bool reallyDead = default, MethodRpc.RpcInfo rpcInfo = default!)
    {
        if (Zombie == null)
        {
            rpcInfo.Local.CancelSend = true;
            return;
        }

        if (Zombie.mZombieType.IsGravestoneOrTarget())
        {
            rpcInfo.Local.CancelSend = true;
            return;
        }

        if (Dying)
        {
            rpcInfo.Local.CancelSend = true;
            return;
        }

        if (rpcInfo.IsLocal)
        {
            reallyDead = Zombie.mBodyHealth <= 1800;
            rpcInfo.Local.Args[0] = reallyDead;
            if (reallyDead)
            {
                Dying = true;
                LogicComponent.OnDeath(Zombie, DeathReason.Burned);
                DespawnAndDestroy(true);
            }
        }
        else
        {
            if (reallyDead)
            {
                Dying = true;
                LogicComponent.OnDeath(Zombie, DeathReason.Burned);
                Zombie?.ApplyBurnOriginal();
                IsReadyToDespawn = true;
            }
        }
    }

    [HideFromIl2Cpp]
    [MethodRpc(false, LocalExecution.None)]
    internal void RpcSnapToPos(float posX, MethodRpc.RpcInfo rpcInfo = default!)
    {
        if (rpcInfo.Sender?.Team != PlayerTeam.Zombies)
        {
            return;
        }

        Zombie?.mPosX = posX;
    }

    [MethodRpc(LocalExecution.None)]
    internal void RpcSetState(string state)
    {
        if (state == ReplantedOnlineMod.Constants.Network.ObjectStates.NULL_STATE)
        {
            State = null;
            return;
        }

        State = state;
    }

    [MethodRpc(LocalExecution.None)]
    internal void RpcMoveToRow(PackedInt row)
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
            packetWriter.WriteBoardUnitX(BoardUnitX);
            packetWriter.WriteBoardUnitY(BoardUnitY);

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
            BoardUnitX = packetReader.ReadBoardUnitX();
            BoardUnitY = packetReader.ReadBoardUnitY();

            var zombie = SeedPacketDefinitions.SpawnZombie(ZombieType, BoardUnitX, BoardUnitY, SpawnType, false).Zombie;
            _z.SetTarget(() => zombie?.mController?.m_zombie);

            OnInit();

            LogicComponent.Deserialize(packetReader, init);

            return;
        }

        LogicComponent.Deserialize(packetReader, init);
    }
}
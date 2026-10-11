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
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Sync;
using ReplantedOnline.Structs.Network;
using ReplantedOnline.Structs.Reloaded;
using ReplantedOnline.Utilities.Modded;
using ReplantedOnline.Utilities.Unity;
using UnityEngine;
using static ReplantedOnline.Attributes.Network.ObjectMessageHandlerAttribute;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay;

/// <summary>
/// Represents a networked zombie on the board, handling synchronization of zombie state
/// </summary>
[RegisterTypeInIl2Cpp]
internal sealed class ZombieNetworked : NetworkObject
{
    private enum ZombieMsgs : byte
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

    internal void SendTakeDamageMessage(PackedInt theDamage, DamageFlags theDamageFlags)
    {
        SendMessage(ZombieMsgs.TakeDamage, theDamage, theDamageFlags);
    }

    [ObjectMessageHandler(ZombieMsgs.TakeDamage)]
    internal void HandleTakeDamageMessage(PackedInt theDamage, DamageFlags damageFlags)
    {
        if (Zombie == null)
            return;

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

    internal void SendDeathMessage(DamageFlags damageFlags)
    {
        if (!Dying)
        {
            Dying = true;
            LogicComponent.OnDeath(Zombie, DeathReason.Normal);
            SendMessage(ZombieMsgs.Death, damageFlags);
        }

        DespawnAndDestroyWhenNullOrDead(true);
    }

    [ObjectMessageHandler(ZombieMsgs.Death)]
    private void HandleDeathMessage(DamageFlags damageFlags)
    {
        if (!Dying)
        {
            Dying = true;
            LogicComponent.OnDeath(Zombie, DeathReason.Normal);
            Zombie?.PlayDeathAnimOriginal(damageFlags);
        }

        IsReadyToDespawn = true;
    }

    internal void SendDieLootMessage(bool withLoot)
    {
        if (!Dying)
        {
            Dying = true;
            LogicComponent.OnDeath(Zombie, DeathReason.Normal);
            SendMessage(ZombieMsgs.DieLoot, withLoot);
        }

        DespawnAndDestroy(true);
    }

    [ObjectMessageHandler(ZombieMsgs.DieLoot)]
    private void HandleDieLootMessage(bool withLoot)
    {
        if (!Dying)
        {
            Dying = true;
            LogicComponent.OnDeath(Zombie, DeathReason.Normal);
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

    internal void SendDragUnderMessage()
    {
        SendMessage(ZombieMsgs.DragUnder);
    }

    [ObjectMessageHandler(ZombieMsgs.DragUnder)]
    private void HandleDragUnderMessage()
    {
        Zombie?.DragUnderOriginal();
    }

    internal void SendMowDownMessage()
    {
        if (!Dying)
        {
            Dying = true;
            LogicComponent.OnDeath(Zombie, DeathReason.Normal);
            SendMessage(ZombieMsgs.MowDown);
        }

        DespawnAndDestroy(true);
    }

    [ObjectMessageHandler(ZombieMsgs.MowDown)]
    private void HandleMowDownMessage()
    {
        if (!Dying)
        {
            Dying = true;
            LogicComponent.OnDeath(Zombie, DeathReason.Normal);
            Zombie?.MowDownOriginal();
        }

        IsReadyToDespawn = true;
    }

    internal void SendSetPlantTargetMessage(Plant target)
    {
        if (Target != target)
        {
            Target = target;
            SendMessage(ZombieMsgs.SetPlantTarget, target);
        }
    }

    [ObjectMessageHandler(ZombieMsgs.SetPlantTarget)]
    private void HandleSetPlantTargetMessage(Plant target)
    {
        Target = target;
    }

    internal void SendEnteringHouseMessage(float xPos)
    {
        EnteringHouse = true;
        SendMessage(ZombieMsgs.EnteringHouse, xPos);
    }

    [ObjectMessageHandler(ZombieMsgs.EnteringHouse)]
    private void HandleEnteringHouseMessage(float xPos)
    {
        EnteringHouse = true;
        VersusGameplayManager.EndGame(Zombie?.mController?.transform.position ?? Vector3.zero, PlayerTeam.Zombies);
        if (Zombie != null)
        {
            Zombie.mPosX = xPos;
            Zombie.mBoard.mCutScene.StartZombiesWon();
        }
    }

    internal void SendMindControlledMessage()
    {
        State = ReplantedOnlineMod.Constants.Network.ObjectStates.ZOMBIE_MIND_CONTROLLED_STATE;
        SendMessage(ZombieMsgs.MindControlled);
        DespawnAndDestroyWhenNullOrDead();
    }

    [ObjectMessageHandler(ZombieMsgs.MindControlled)]
    private void HandleMindControlledMessage()
    {
        State = ReplantedOnlineMod.Constants.Network.ObjectStates.ZOMBIE_MIND_CONTROLLED_STATE;
        Zombie?.StartMindControlledOriginal();
    }

    internal void SendSetFrozenMessage(bool hitIceTrap)
    {
        int counter;
        if (Zombie == null)
        {
            counter = 0;
        }
        else if (hitIceTrap)
        {
            counter = Zombie.mIceTrapCounter;
        }
        else
        {
            counter = Zombie.mChilledCounter;
        }

        SendMessage(ZombieMsgs.SetFrozen, hitIceTrap, counter);
    }

    [ObjectMessageHandler(ZombieMsgs.SetFrozen)]
    private void HandleSetFrozenMessage(bool hitIceTrap, PackedInt counter)
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

    internal void SendApplyBurnMessage()
    {
        if (Zombie == null)
            return;

        bool reallyDead = Zombie.mBodyHealth <= 1800;

        SendMessage(ZombieMsgs.ApplyBurn, reallyDead);

        if (reallyDead && !Dying)
        {
            Dying = true;
            LogicComponent.OnDeath(Zombie, DeathReason.Burned);
            DespawnAndDestroy(true);
        }
    }

    [ObjectMessageHandler(ZombieMsgs.ApplyBurn)]
    private void HandleApplyBurnMessage(bool reallyDead)
    {
        if (Zombie == null)
            return;

        if (Zombie.mZombieType.IsGravestoneOrTarget())
            return;

        if (reallyDead && !Dying)
        {
            Dying = true;
            LogicComponent.OnDeath(Zombie, DeathReason.Burned);
            Zombie?.ApplyBurnOriginal();
            IsReadyToDespawn = true;
        }
    }

    internal void SendSnapToPosMessage()
    {
        SendMessage(ZombieMsgs.SnapToPos, Zombie?.mPosX ?? 0);
    }

    [ObjectMessageHandler(ZombieMsgs.SnapToPos, false)]
    [HideFromIl2Cpp]
    private void HandleSnapToPosMessage(float posX, MessageInfo rpcInfo)
    {
        if (rpcInfo.Sender.Team != PlayerTeam.Zombies)
            return;

        Zombie?.mPosX = posX;
    }

    internal void SendSetStateMessage(string state)
    {
        SendMessage(ZombieMsgs.SetState, state);
    }

    [ObjectMessageHandler(ZombieMsgs.SetState)]
    private void HandleSetStateMessage(string state)
    {
        if (state == ReplantedOnlineMod.Constants.Network.ObjectStates.NULL_STATE)
        {
            State = null;
            return;
        }

        State = state;
    }

    internal void SendMoveToRowMessage(PackedInt row)
    {
        SendMessage(ZombieMsgs.MoveToRow, row);
    }

    [ObjectMessageHandler(ZombieMsgs.MoveToRow)]
    private void HandleMoveToRowMessage(PackedInt row)
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
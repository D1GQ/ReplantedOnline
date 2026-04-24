using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Monos;
using ReplantedOnline.Network.Client.Object.Replanted.Components;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;
using ReplantedOnline.Utilities;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Network.Client.Object.Replanted;

/// <summary>
/// Represents a networked zombie on the board, handling synchronization of zombie state
/// across connected clients including health, position, and follower relationships.
/// </summary>
internal sealed class ZombieNetworked : NetworkObject
{
    private enum ZombieRpcs : byte
    {
        TakeDamage,
        Death,
        DieLoot,
        MowDown,
        SetPlantTarget,
        EnteringHouse,
        MindControlled,
        SetFrozen,
        ApplyBurn,
        SetState
    }

    /// <summary>
    /// Gets the current target zombie.
    /// </summary>
    [HideFromIl2Cpp]
    internal Plant Target { get; set; }

    /// <summary>
    /// The underlying zombie instance that this networked object represents.
    /// </summary>
    internal Zombie _Zombie;

    /// <summary>
    /// The type of zombie this networked object represents when spawning.
    /// </summary>
    internal ZombieType ZombieType;

    /// <summary>
    /// If the bush on the row the zombie spawns in shakes
    /// </summary>
    internal bool ShakeBush;

    /// <summary>
    /// The grid X coordinate where this zombie is located when spawning.
    /// </summary>
    internal int GridX;

    /// <summary>
    /// The grid Y coordinate where this zombie is located when spawning.
    /// </summary>
    internal int GridY;

    internal ZombieNetworkComponent LogicComponent;
    internal bool EnteringHouse;

    public override string GetObjectName()
    {
        return $"{Enum.GetName(_Zombie.mZombieType)}Zombie ({NetworkId})";
    }

    [HideFromIl2Cpp]
    protected override void OnClone(RuntimePrefab prefab)
    {
        var networkedDebugger = gameObject.AddComponent<NetworkedDebugger>();
        networkedDebugger.Initialize(this);
    }

    public override void OnInit()
    {
        LogicComponent = ZombieNetworkComponent.AddComponent(this, ZombieType);
        _Zombie.AddNetworkedLookup(this);
    }

    private void OnDestroy()
    {
        this.RemoveNetworkedLookup();

        if (_Zombie != null)
        {
            if (!Dead && !_Zombie.IsDeadOrDying())
            {
                CheckDeath();
                _Zombie.DieDeserialize();
            }
        }
    }

    private void Update()
    {
        if (!IsOnNetwork) return;

        if (AmOwner)
        {
            if (ZombieType == ZombieType.Bungee || State is NetStates.ZOMBIE_MIND_CONTROLLED_STATE)
            {
                if (_Zombie == null)
                {
                    DespawnAndDestroy();
                }
            }
        }

        if (_Zombie == null) return;

        if (_Zombie.mDead && !Dead)
        {
            CheckDeath();
            _Zombie.RemoveNetworkedLookup();
            _Zombie = null;
            return;
        }

        if (ZombieType == ZombieType.Gravestone)
        {
            return;
        }

        LogicComponent.Update();
    }

    [HideFromIl2Cpp]
    internal void CheckDeath()
    {
        if (_Zombie.mZombieType == ZombieType.Gravestone)
        {
            Instances.GameplayActivity.Board.m_vsGravestones.Remove(_Zombie);
            _Zombie.mGraveX = 0;
            _Zombie.mGraveY = 0;
        }
        else if (_Zombie.mZombieType == ZombieType.Target)
        {
            Instances.GameplayActivity.VersusMode.ZombieLife--;

            if (Instances.GameplayActivity.VersusMode.ZombieLife == 0)
            {
                if (_Zombie?.mController == null) return;
                VersusGameplayManager.EndGame(_Zombie.mController.transform.position, PlayerTeam.Plants);
            }
        }
    }

    // For some reason TakeDamage gets triggered twice naturally, so we must not send the rpc on the second time using damageInterval.
    private uint damageInterval;
    internal void SendTakeDamageRpc(int theDamage, DamageFlags theDamageFlags)
    {
        damageInterval++;
        if (damageInterval % 2 != 0)
        {
            SendNetworkObjectRpc(ZombieRpcs.TakeDamage, theDamage, theDamageFlags);
        }
    }

    [RpcHandler(ZombieRpcs.TakeDamage)]
    internal void HandleTakeDamageRpc(int theDamage, DamageFlags damageFlags)
    {
        int minimumHealth = 5;
        int relevantHealth;

        if ((damageFlags & DamageFlags.Spike) == DamageFlags.Spike ||
            (damageFlags & DamageFlags.BypassesShield) == DamageFlags.BypassesShield)
        {
            relevantHealth = _Zombie.mBodyHealth + _Zombie.mHelmHealth;
        }
        else
        {
            relevantHealth = _Zombie.mBodyHealth + _Zombie.mHelmHealth + _Zombie.mShieldHealth;
        }

        if (theDamage >= relevantHealth - (minimumHealth - 1))
        {
            int damageToApply = relevantHealth - minimumHealth;

            if (damageToApply > 0)
            {
                _Zombie.TakeDamageOriginal(damageToApply, damageFlags);
            }
        }
        else
        {
            _Zombie.TakeDamageOriginal(theDamage, damageFlags);
        }
    }

    internal bool Dead;
    internal void SendDeathRpc(DamageFlags damageFlags)
    {
        if (!Dead)
        {
            Dead = true;
            SendNetworkObjectRpc(ZombieRpcs.Death, damageFlags);
            DespawnAndDestroy();
        }
    }

    [RpcHandler(ZombieRpcs.Death)]
    internal void HandleDeathRpc(DamageFlags damageFlags)
    {
        if (!Dead)
        {
            Dead = true;
            CheckDeath();
            _Zombie.PlayDeathAnimOriginal(damageFlags);
        }
    }

    internal void SendDieLootRpc(bool withLoot)
    {
        if (!Dead)
        {
            Dead = true;
            SendNetworkObjectRpc(ZombieRpcs.DieLoot, withLoot);
            DespawnAndDestroy();
        }
    }

    [RpcHandler(ZombieRpcs.DieLoot)]
    internal void HandleDieLootRpc(bool withLoot)
    {
        if (!Dead)
        {
            Dead = true;
            CheckDeath();
            if (withLoot)
            {
                _Zombie.DieWithLootOriginal();
            }
            else
            {
                _Zombie.DieNoLootOriginal();
            }
        }
    }

    internal void SendMowDownRpc()
    {
        if (!Dead)
        {
            Dead = true;
            SendNetworkObjectRpc(ZombieRpcs.MowDown);
            DespawnAndDestroy();
        }
    }

    [RpcHandler(ZombieRpcs.MowDown)]
    internal void HandleMowDownRpc()
    {
        if (!Dead)
        {
            Dead = true;
            _Zombie.MowDownOriginal();
        }
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
    internal void HandleSetPlantTargetRpc(Plant target)
    {
        Target = target;
    }

    internal void SendEnteringHouseRpc(float xPos)
    {
        EnteringHouse = true;
        SendNetworkObjectRpc(ZombieRpcs.EnteringHouse, xPos);
    }

    [RpcHandler(ZombieRpcs.EnteringHouse)]
    internal void HandleEnteringHouseRpc(float xPos)
    {
        EnteringHouse = true;
        LogicComponent.StopLarpPos();
        _Zombie?.mPosX = xPos;
        VersusGameplayManager.EndGame(_Zombie.mController.transform.position, PlayerTeam.Zombies);
    }

    internal void SendMindControlledRpc()
    {
        State = NetStates.ZOMBIE_MIND_CONTROLLED_STATE;
        SendNetworkObjectRpc(ZombieRpcs.MindControlled);
    }

    [RpcHandler(ZombieRpcs.MindControlled)]
    internal void HandleMindControlledRpc()
    {
        State = NetStates.ZOMBIE_MIND_CONTROLLED_STATE;
        _Zombie.StartMindControlledOriginal();
    }

    internal void SendSetFrozenRpc(bool frozen)
    {
        this.StartCoroutine(CoroutineUtils.WaitForCondition(() => !frozen || _Zombie.mChilledCounter > 0, () =>
        {
            int counter;
            if (frozen)
            {
                counter = _Zombie.mIceTrapCounter;
            }
            else
            {
                counter = _Zombie.mChilledCounter;
            }

            SendNetworkObjectRpc(ZombieRpcs.SetFrozen, frozen, counter);
        }));
    }

    [RpcHandler(ZombieRpcs.SetFrozen)]
    internal void HandleSetFrozenRpc(bool frozen, int counter)
    {
        if (frozen)
        {
            LogicComponent.StopLarpPos();
            _Zombie.HitIceTrapOriginal();
            _Zombie.mIceTrapCounter = counter;
        }
        else
        {
            _Zombie.RemoveIceTrapOriginal();
            _Zombie.mChilledCounter = counter;
        }
    }

    internal void SendApplyBurnRpc()
    {
        bool reallyDead = _Zombie.mBodyHealth <= 1800;

        SendNetworkObjectRpc(ZombieRpcs.ApplyBurn, reallyDead);

        if (reallyDead && !Dead)
        {
            Dead = true;
            DespawnAndDestroy();
        }
    }

    [RpcHandler(ZombieRpcs.ApplyBurn)]
    internal void HandleApplyBurnRpc(bool reallyDead)
    {
        if (_Zombie.mZombieType.IsGravestoneOrTarget()) return;

        Dead = Dead || reallyDead;
        _Zombie.ApplyBurnOriginal();
    }

    internal void SendSetStateRpc(string state)
    {
        SendNetworkObjectRpc(ZombieRpcs.SetState, state);
    }

    [RpcHandler(ZombieRpcs.SetState)]
    internal void HandleSetStateRpc(string state)
    {
        if (state == NetStates.NULL_STATE)
        {
            State = null;
            return;
        }

        State = state;
    }

    [HideFromIl2Cpp]
    public override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init)
        {
            // Set spawn info
            packetWriter.WriteInt(GridX);
            packetWriter.WriteInt(GridY);
            packetWriter.WriteBool(ShakeBush);
            packetWriter.WriteInt((int)ZombieType);

            LogicComponent.Serialize(packetWriter, init);
            ClearDirtyBits();

            return;
        }

        LogicComponent.Serialize(packetWriter, init);
        ClearDirtyBits();
    }

    [HideFromIl2Cpp]
    public override void Deserialize(PacketReader packetReader, bool init)
    {
        if (init)
        {
            // Read spawn info
            GridX = packetReader.ReadInt();
            GridY = packetReader.ReadInt();
            ShakeBush = packetReader.ReadBool();
            ZombieType = (ZombieType)packetReader.ReadInt();

            _Zombie = SeedPacketDefinitions.SpawnZombie(ZombieType, GridX, GridY, ShakeBush, false);

            OnInit();

            LogicComponent.Deserialize(packetReader, init);

            return;
        }

        LogicComponent.Deserialize(packetReader, init);
    }
}
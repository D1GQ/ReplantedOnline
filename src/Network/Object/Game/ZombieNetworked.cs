using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Monos;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Server.Packet;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;
using ReplantedOnline.Patches.Gameplay.Versus.Zombies;
using ReplantedOnline.Utilities;
using System.Collections;
using UnityEngine;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Network.Object.Game;

/// <summary>
/// Represents a networked zombie on the board, handling synchronization of zombie state
/// across connected clients including health, position, and follower relationships.
/// </summary>
internal sealed class ZombieNetworked : NetworkObject
{
    internal enum ZombieRpcs
    {
        TakeDamage,
        Death,
        DieWithLoot,
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
    /// Represents the networked animation controller used to synchronize animation states across multiple clients.
    /// </summary>
    internal AnimationControllerNetworked AnimationControllerNetworked;

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

    public override string GetObjectName()
    {
        return $"{Enum.GetName(_Zombie.mZombieType)}Zombie ({NetworkId})";
    }

    [HideFromIl2Cpp]
    protected override void OnClone(RuntimePrefab prefab)
    {
        AnimationControllerNetworked = gameObject.AddComponent<AnimationControllerNetworked>();
        AddChild(AnimationControllerNetworked);

        var networkedDebugger = gameObject.AddComponent<NetworkedDebugger>();
        networkedDebugger.Initialize(this);
    }

    public void OnDestroy()
    {
        if (_Zombie != null)
        {
            _Zombie.RemoveNetworkedLookup();

            if (!Dead && !_Zombie.IsDeadOrDying())
            {
                _Zombie.DieDeserialize();
            }
        }
    }

    public void Update()
    {
        if (!IsOnNetwork) return;

        if (AmOwner)
        {
            if (ZombieType == ZombieType.Bungee || State is NetStates.MIND_CONTROLLED_STATE)
            {
                if (_Zombie == null)
                {
                    DespawnAndDestroy();
                }
            }
        }

        if (_Zombie == null) return;

        if (_Zombie.mDead)
        {
            _Zombie.RemoveNetworkedLookup();
            _Zombie = null;
            return;
        }

        switch (ZombieType)
        {
            case ZombieType.Gravestone:
                return;
            case ZombieType.Bungee:
                BungeeUpdate();
                return;
            case ZombieType.Digger:
                if (_Zombie.mZombiePhase is ZombiePhase.DiggerWalking or ZombiePhase.DiggerWalkingWithoutAxe)
                {
                    NormalUpdate();
                }
                return;
            case ZombieType.Bobsled:
                if (_Zombie.mZombiePhase == ZombiePhase.ZombieNormal)
                {
                    NormalUpdate();
                }
                return;
            case ZombieType.Imp:
                if (_Zombie.mZombiePhase is not (ZombiePhase.ImpGettingThrown or ZombiePhase.ImpLanding))
                {
                    NormalUpdate();
                }
                return;
            case ZombieType.JackInTheBox:
                JackInTheBoxUpdate();
                break;
            case ZombieType.Polevaulter:
                PolevaulterUpdate();
                if (_Zombie.mZombiePhase is not ZombiePhase.PolevaulterInVault)
                {
                    NormalUpdate();
                }
                return;
            case ZombieType.Ladder:
                LadderUpdate();
                break;
            case ZombieType.Catapult:
                CatapultUpdate();
                break;
        }

        NormalUpdate();
    }

    internal bool EnteringHouse;
    private float _syncCooldown = 2f;
    private float _lastPos;
    private void NormalUpdate()
    {
        if (_Zombie == null) return;

        if (AmOwner)
        {
            if (!_Zombie.mDead)
            {
                if (_syncCooldown <= 0f && _lastPos != _Zombie.mPosX)
                {
                    MarkDirty();
                    _syncCooldown = 2f;
                    _lastPos = _Zombie.mPosX;
                }
                _syncCooldown -= Time.deltaTime;
            }
        }
        else
        {
            if (!EnteringHouse)
            {
                if (_Zombie.mPosX <= 0f)
                {
                    _Zombie.mPosX = 0f;
                }
            }
        }
    }

    private void BungeeUpdate()
    {
        if (_Zombie == null) return;

        if (AmOwner)
        {
            if (_Zombie.mZombiePhase == ZombiePhase.BungeeGrabbing && _Zombie.mPhaseCounter < 10 && State is not NetStates.UPDATE_STATE)
            {
                State = NetStates.UPDATE_STATE;
                SendSetStateRpc(NetStates.UPDATE_STATE);
            }
        }
        else
        {
            if (_Zombie.mZombiePhase == ZombiePhase.BungeeGrabbing)
            {
                if (State is not NetStates.UPDATE_STATE)
                {
                    _Zombie.mPhaseCounter = int.MaxValue;
                }
                else
                {
                    _Zombie.mPhaseCounter = 0;
                }
            }
        }
    }

    private void JackInTheBoxUpdate()
    {
        if (_Zombie == null) return;

        if (AmOwner)
        {
            if (_Zombie.mZombiePhase == ZombiePhase.JackInTheBoxPopping && State is not NetStates.UPDATE_STATE)
            {
                Dead = true;
                State = NetStates.UPDATE_STATE;
                SendSetStateRpc(NetStates.UPDATE_STATE);
                this.StartCoroutine(CoroutineUtils.WaitForCondition(() => _Zombie == null || _Zombie.mDead == true, () =>
                {
                    DespawnAndDestroy();
                }));
            }
        }
        else
        {
            if (_Zombie.mZombiePhase == ZombiePhase.JackInTheBoxRunning)
            {
                if (State is not NetStates.UPDATE_STATE)
                {
                    _Zombie.mPhaseCounter = int.MaxValue;
                }
                else
                {
                    Dead = true;
                    _Zombie.mPhaseCounter = 0;
                }
            }
        }
    }

    private void PolevaulterUpdate()
    {
        if (_Zombie == null) return;

        if (_Zombie.mZombiePhase == ZombiePhase.RisingFromGrave) return;

        if (AmOwner)
        {
            if (_Zombie.mZombiePhase == ZombiePhase.PolevaulterInVault && Target == null)
            {
                // Send target to vault
                Plant target = _Zombie.FindPlantTarget(ZombieAttackType.Vault);
                SendSetPlantTargetRpc(target);
            }
        }

        // Non owner logic is handled in PolevaulterZombiePatch.cs

        if (_Zombie.mZombiePhase == ZombiePhase.PolevaulterPostVault)
        {
            Target = null;
        }
    }

    private void LadderUpdate()
    {
        if (_Zombie == null) return;

        if (_Zombie.mZombiePhase == ZombiePhase.RisingFromGrave) return;

        if (AmOwner)
        {
            if (_Zombie.mZombiePhase == ZombiePhase.LadderPlacing && Target == null)
            {
                // Send target to place ladder
                Plant target = _Zombie.FindPlantTarget(ZombieAttackType.Ladder);
                SendSetPlantTargetRpc(target);
            }
            else if (_Zombie.mZombiePhase == ZombiePhase.ZombieNormal)
            {
                if (State is not NetStates.LADDER_ZOMBIE_PLACED_LADDER)
                {
                    State = NetStates.LADDER_ZOMBIE_PLACED_LADDER;
                    SendSetStateRpc(NetStates.LADDER_ZOMBIE_PLACED_LADDER);
                }
            }
        }
        else
        {
            if (_Zombie.mZombiePhase == ZombiePhase.LadderPlacing && _Zombie.mPhaseCounter == 0)
            {
                if (State is NetStates.LADDER_ZOMBIE_PLACED_LADDER)
                {
                    _Zombie.mZombiePhase = ZombiePhase.ZombieNormal;
                    _Zombie.DetachShield();
                    State = null;
                }
            }

            // Rest of non owner logic is handled in LadderZombiePatch.cs
        }

        if (_Zombie.mZombiePhase == ZombiePhase.ZombieNormal)
        {
            Target = null;
        }
    }

    private void CatapultUpdate()
    {
        if (_Zombie == null) return;

        if (AmOwner)
        {
            var target = _Zombie.FindCatapultTarget();
            if (Target != target)
            {
                SendSetPlantTargetRpc(target);
            }
        }
    }

    [HideFromIl2Cpp]
    internal void CheckDeath(Action callback)
    {
        if (_Zombie.mZombieType == ZombieType.Gravestone)
        {
            Instances.GameplayActivity.Board.m_vsGravestones.Remove(_Zombie);
            _Zombie.mGraveX = 0;
            _Zombie.mGraveY = 0;
            callback();
        }
        else if (_Zombie.mZombieType == ZombieType.Target)
        {
            Instances.GameplayActivity.VersusMode.ZombieLife--;

            if (Instances.GameplayActivity.VersusMode.ZombieLife > 0)
            {
                callback();
            }
            else
            {
                VersusGameplayManager.EndGame(_Zombie?.mController?.gameObject, PlayerTeam.Plants);
                callback();
            }
        }
        else
        {
            callback();
        }
    }

    // For some reason TakeDamage gets triggered twice naturally, so we must not send the rpc on the second time using damageInterval.
    private int damageInterval;
    internal void SendTakeDamageRpc(int theDamage, DamageFlags theDamageFlags)
    {
        damageInterval++;
        if (damageInterval % 2 != 0)
        {
            var writer = PacketWriter.Get();
            writer.WriteInt(theDamage);
            writer.WriteByte((byte)theDamageFlags);
            SendNetworkClassRpc((byte)ZombieRpcs.TakeDamage, writer);
            writer.Recycle();
        }
    }

    private void HandleTakeDamageRpc(int theDamage, DamageFlags damageFlags)
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
            var writer = PacketWriter.Get();
            writer.WriteByte((byte)damageFlags);
            SendNetworkClassRpc((byte)ZombieRpcs.Death, writer);
            writer.Recycle();
            DespawnAndDestroy();
        }
    }

    private void HandleDeathRpc(DamageFlags damageFlags)
    {
        if (!Dead)
        {
            Dead = true;
            CheckDeath(() =>
            {
                _Zombie.PlayDeathAnimOriginal(damageFlags);
            });
        }
    }

    internal void SendDieWithLootRpc()
    {
        if (!Dead)
        {
            Dead = true;
            SendNetworkClassRpc((byte)ZombieRpcs.DieWithLoot);
            DespawnAndDestroy();
        }
    }

    private void HandleDieWithLootRpc()
    {
        if (!Dead)
        {
            Dead = true;
            _Zombie.DieWithLootOriginal();
        }
    }

    internal void SendMowDownRpc()
    {
        if (!Dead)
        {
            Dead = true;
            SendNetworkClassRpc((byte)ZombieRpcs.MowDown);
            DespawnAndDestroy();
        }
    }

    private void HandleMowDownRpc()
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
            var writer = PacketWriter.Get();
            writer.WriteNetworkObject(target?.GetNetworked());
            SendNetworkClassRpc((byte)ZombieRpcs.SetPlantTarget, writer);
            writer.Recycle();
        }
    }

    private void HandleSetPlantTargetRpc(Plant target)
    {
        Target = target;
    }

    internal void SendEnteringHouseRpc(float xPos)
    {
        EnteringHouse = true;
        var writer = PacketWriter.Get();
        writer.WriteFloat(xPos);
        SendNetworkClassRpc((byte)ZombieRpcs.EnteringHouse, writer);
        writer.Recycle();
    }

    private void HandleEnteringHouseRpc(float xPos)
    {
        EnteringHouse = true;
        StopLarpPos();
        _Zombie?.mPosX = xPos;
        VersusGameplayManager.EndGame(_Zombie?.mController?.gameObject, PlayerTeam.Zombies);
    }

    internal void SendMindControlledRpc()
    {
        State = NetStates.MIND_CONTROLLED_STATE;
        SendNetworkClassRpc((byte)ZombieRpcs.MindControlled);
    }

    private void HandleMindControlledRpc()
    {
        State = NetStates.MIND_CONTROLLED_STATE;
        _Zombie.StartMindControlledOriginal();
    }

    internal void SendSetFrozenRpc(bool frozen)
    {
        this.StartCoroutine(CoroutineUtils.WaitForCondition(() => !frozen || _Zombie.mChilledCounter > 0, () =>
        {
            var writer = PacketWriter.Get();
            writer.WriteBool(frozen);
            if (frozen)
            {
                writer.WriteInt(_Zombie.mIceTrapCounter);
            }
            else
            {
                writer.WriteInt(_Zombie.mChilledCounter);
            }
            SendNetworkClassRpc((byte)ZombieRpcs.SetFrozen, writer);
            writer.Recycle();
        }));
    }

    private void HandleSetFrozenRpc(bool frozen, int counter)
    {
        if (frozen)
        {
            StopLarpPos();
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

        var writer = PacketWriter.Get();
        writer.WriteBool(reallyDead);
        SendNetworkClassRpc((byte)ZombieRpcs.ApplyBurn, writer);
        writer.Recycle();

        if (reallyDead && !Dead)
        {
            Dead = true;
            DespawnAndDestroy();
        }
    }

    private void HandleApplyBurnRpc(bool reallyDead)
    {
        // For some reason the first target zombie that spawns in likes to get burned, i have no idea why...
        if (_Zombie.mZombieType is ZombieType.Target) return;

        Dead = Dead || reallyDead;
        _Zombie.ApplyBurnOriginal();
    }

    internal void SendSetStateRpc(string state)
    {
        var writer = PacketWriter.Get();
        writer.WriteString(state);
        SendNetworkClassRpc((byte)ZombieRpcs.SetState, writer);
        writer.Recycle();
    }

    private void HandleSetStateRpc(string state)
    {
        State = state;
    }

    [HideFromIl2Cpp]
    public override void HandleRpc(NetClient sender, byte rpcId, PacketReader packetReader)
    {
        if (sender.ClientId != OwnerId) return;

        var rpc = (ZombieRpcs)rpcId;
        switch (rpc)
        {
            case ZombieRpcs.TakeDamage:
                {
                    var theDamage = packetReader.ReadInt();
                    var damageFlags = (DamageFlags)packetReader.ReadByte();
                    HandleTakeDamageRpc(theDamage, damageFlags);
                }
                break;
            case ZombieRpcs.Death:
                {
                    var damageFlags = (DamageFlags)packetReader.ReadByte();
                    HandleDeathRpc(damageFlags);
                }
                break;
            case ZombieRpcs.DieWithLoot:
                {
                    HandleDieWithLootRpc();
                }
                break;
            case ZombieRpcs.MowDown:
                {
                    HandleMowDownRpc();
                }
                break;
            case ZombieRpcs.SetPlantTarget:
                {
                    var target = packetReader.ReadNetworkObject<PlantNetworked>();
                    HandleSetPlantTargetRpc(target?._Plant);
                }
                break;
            case ZombieRpcs.EnteringHouse:
                {
                    var xPos = packetReader.ReadFloat();
                    HandleEnteringHouseRpc(xPos);
                }
                break;
            case ZombieRpcs.MindControlled:
                {
                    HandleMindControlledRpc();
                }
                break;
            case ZombieRpcs.SetFrozen:
                {
                    var frozen = packetReader.ReadBool();
                    var counter = packetReader.ReadInt();
                    HandleSetFrozenRpc(frozen, counter);
                }
                break;
            case ZombieRpcs.ApplyBurn:
                {
                    bool reallyDead = packetReader.ReadBool();
                    HandleApplyBurnRpc(reallyDead);
                }
                break;
            case ZombieRpcs.SetState:
                {
                    var state = packetReader.ReadString();
                    HandleSetStateRpc(state);
                }
                break;
        }
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

            if (ZombieType == ZombieType.Imp)
            {
                GargantuarZombiePatch.ImpSerialize(_Zombie, packetWriter);
            }

            if (ZombieType == ZombieType.Bobsled)
            {
                BobsledZombiePatch.BobsledSerialize(_Zombie, packetWriter);
            }

            return;
        }

        packetWriter.WriteInt(_Zombie.mRow);
        packetWriter.WriteFloat(_Zombie.mVelX);
        packetWriter.WriteFloat(_Zombie.mPosX);

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
            _Zombie.AddNetworkedLookup(this);
            AnimationControllerNetworked.Init(_Zombie.mController.AnimationController);

            if (ZombieType == ZombieType.Imp)
            {
                GargantuarZombiePatch.ImpDeserialize(_Zombie, packetReader);
            }

            if (ZombieType == ZombieType.Bobsled)
            {
                BobsledZombiePatch.BobsledDeserialize(_Zombie, packetReader);
            }

            return;
        }

        if (!AmOwner)
        {
            _Zombie.mRow = packetReader.ReadInt();
            _Zombie.mVelX = packetReader.ReadFloat();
            _Zombie.UpdateAnimSpeed();
            var posX = packetReader.ReadFloat();
            lastSyncPosX = posX;
            LarpPos(posX);
        }
    }

    private Coroutine larpCoroutine;
    internal float? lastSyncPosX;

    /// <summary>
    /// Smoothly interpolates the zombie's position to the target position when distance threshold is exceeded.
    /// </summary>
    /// <param name="posX">The target X position to interpolate to</param>
    private void LarpPos(float posX)
    {
        if (_Zombie == null || EnteringHouse || posX < 15f) return;

        float currentX = _Zombie.mPosX;
        float distance = Mathf.Abs(currentX - posX);

        // Calculate threshold based on velocity (0.5 seconds of movement)
        float threshold = Mathf.Abs(_Zombie.mVelX) * 0.3f;
        threshold = Mathf.Clamp(threshold, 10f, 50f);

        if (distance > threshold)
        {
            // Stop existing interpolation
            StopLarpPos();

            if (distance < 100f && _Zombie.mZombieType != ZombieType.Pogo)
            {
                larpCoroutine = this.StartCoroutine(CoLarpPos(posX));
            }
            else
            {
                _Zombie.mPosX = posX;
            }
        }
    }

    /// <summary>
    /// Stop larping to network pos
    /// </summary>
    private void StopLarpPos()
    {
        if (larpCoroutine != null)
        {
            lastSyncPosX = null;
            StopCoroutine(larpCoroutine);
        }
    }

    /// <summary>
    /// Coroutine that smoothly interpolates the zombie's position over time.
    /// </summary>
    /// <param name="targetX">The target X position to reach</param>
    [HideFromIl2Cpp]
    private IEnumerator CoLarpPos(float targetX)
    {
        if (this == null || _Zombie == null) yield break;

        float startX = _Zombie.mPosX;
        float distance = Mathf.Abs(targetX - startX);

        // Use zombie's current velocity for interpolation speed
        float speed = Mathf.Abs(_Zombie.mVelX);
        speed = Mathf.Clamp(speed, 10f, 40f);

        float duration = Mathf.Clamp(distance / speed, 0.1f, 2f);

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            if (this == null || _Zombie == null) yield break;

            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            t = SmoothStep(t);

            _Zombie.mPosX = Mathf.Lerp(startX, targetX, t);
            yield return null;
        }

        // Ensure final position is exact
        _Zombie?.mPosX = targetX;

        lastSyncPosX = null;
        larpCoroutine = null;
    }

    private static float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
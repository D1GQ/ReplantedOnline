using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums;
using ReplantedOnline.Helper;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Monos;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;
using System.Collections;
using UnityEngine;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Network.Object.Game;

/// <summary>
/// Represents a networked zombie entity in the game world, handling synchronization of zombie state
/// across connected clients including health, position, and follower relationships.
/// </summary>
internal sealed class ZombieNetworked : NetworkObject
{
    internal enum ZombieRpcs
    {
        TakeDamage,
        Death,
        DieNoLoot,
        EnteringHouse,
        MindControlled,
        SetFrozen,
        ApplyBurn,
        SetState
    }

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
            case ZombieType.JackInTheBox:
                JackInTheBoxUpdate();
                break;
            case ZombieType.Polevaulter:
                PolevaulterUpdate();
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
            if (_Zombie.mZombiePhase is ZombiePhase.BungeeGrabbing && _Zombie.mPhaseCounter < 10 && _State is not States.UpdateState)
            {
                _State = States.UpdateState;
                SendSetStateRpc(States.UpdateState);
            }
        }
        else
        {
            if (_Zombie.mZombiePhase is ZombiePhase.BungeeGrabbing)
            {
                if (_State is not States.UpdateState)
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
            if (_Zombie.mZombiePhase is ZombiePhase.JackInTheBoxPopping && _State is not States.UpdateState)
            {
                Dead = true;
                _State = States.UpdateState;
                SendSetStateRpc(States.UpdateState);
                StartCoroutine(CoroutineUtils.WaitForCondition(() => _Zombie.mDead, () =>
                {
                    DespawnAndDestroy();
                }).WrapToIl2cpp());
            }
        }
        else
        {
            if (_Zombie.mZombiePhase is ZombiePhase.JackInTheBoxRunning)
            {
                if (_State is not States.UpdateState)
                {
                    _Zombie.mPhaseCounter = int.MaxValue;
                }
                else
                {
                    Dead = true;
                    _Zombie.mZombiePhase = ZombiePhase.JackInTheBoxPopping;
                }
            }
        }
    }

    private void PolevaulterUpdate()
    {
        if (_Zombie == null) return;

        if (AmOwner)
        {
            if (_Zombie.mZombiePhase is ZombiePhase.PolevaulterPreVault && _State is not States.UpdateState)
            {
                _State = States.UpdateState;
                SendSetStateRpc(States.UpdateState);
            }
        }
        else
        {
            if (_State is States.UpdateState)
            {
                _State = null;
                _Zombie.mZombiePhase = ZombiePhase.PolevaulterInVault;
            }
        }
    }

    [HideFromIl2Cpp]
    internal void CheckDeath(Action callback)
    {
        if (_Zombie.mZombieType is ZombieType.Gravestone)
        {
            Instances.GameplayActivity.Board.m_vsGravestones.Remove(_Zombie);
            _Zombie.mGraveX = 0;
            _Zombie.mGraveY = 0;
            callback();
        }
        else if (_Zombie.mZombieType is ZombieType.Target)
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

    internal void SendTakeDamageRpc(int theDamage, DamageFlags theDamageFlags)
    {
        var writer = PacketWriter.Get();
        writer.WriteInt(theDamage);
        writer.WriteByte((byte)theDamageFlags);
        SendNetworkClassRpc((byte)ZombieRpcs.TakeDamage, writer);
        writer.Recycle();
    }

    private void HandleTakeDamageRpc(int theDamage, DamageFlags damageFlags)
    {
        // Only die from rpc
        if (((_Zombie.mBodyHealth + _Zombie.mHelmHealth + _Zombie.mShieldHealth) - theDamage) > 1)
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

    internal void SendDieNoLootRpc()
    {
        if (!Dead)
        {
            Dead = true;
            SendNetworkClassRpc((byte)ZombieRpcs.DieNoLoot);
            DespawnAndDestroy();
        }
    }

    private void HandleDieNoLootRpc()
    {
        if (!Dead)
        {
            Dead = true;
            _Zombie.DieNoLoot();
        }
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
        SendNetworkClassRpc((byte)ZombieRpcs.MindControlled);
    }

    private void HandleMindControlledRpc()
    {
        _State = States.MindControlledState;
        _Zombie.StartMindControlled();
    }

    internal void SendSetFrozenRpc(bool frozen)
    {
        StartCoroutine(CoroutineUtils.WaitForCondition(() => !frozen || _Zombie.mChilledCounter > 0, () =>
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
        }).WrapToIl2cpp());
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
        if (!Dead)
        {
            Dead = true;
            SendNetworkClassRpc((byte)ZombieRpcs.ApplyBurn);
            DespawnAndDestroy();
        }
    }

    private void HandleApplyBurnRpc()
    {
        if (!Dead)
        {
            Dead = true;
            _Zombie.ApplyBurnOriginal();
        }
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
        _State = state;
    }

    [HideFromIl2Cpp]
    public override void HandleRpc(SteamNetClient sender, byte rpcId, PacketReader packetReader)
    {
        if (sender.SteamId != OwnerId) return;

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
            case ZombieRpcs.DieNoLoot:
                {
                    HandleDieNoLootRpc();
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
                    HandleApplyBurnRpc();
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
                packetWriter.WriteFloat(_Zombie.mPosX);
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

            _Zombie = Utils.SpawnZombie(ZombieType, GridX, GridY, ShakeBush, false);
            _Zombie.AddNetworkedLookup(this);
            AnimationControllerNetworked.Init(_Zombie.mController.AnimationController);

            gameObject.name = $"{Enum.GetName(_Zombie.mZombieType)}_Zombie ({NetworkId})";

            if (ZombieType == ZombieType.Imp)
            {
                var posX = packetReader.ReadFloat();
                _Zombie.mPosX = posX;
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
                larpCoroutine = StartCoroutine(CoLarpPos(posX).WrapToIl2cpp());
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
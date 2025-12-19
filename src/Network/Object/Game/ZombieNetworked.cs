using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using MelonLoader;
using ReplantedOnline.Enums;
using ReplantedOnline.Helper;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Versus.NetworkSync;
using System.Collections;
using UnityEngine;
using Zombie = Il2CppReloaded.Gameplay.Zombie;

namespace ReplantedOnline.Network.Object.Game;

/// <summary>
/// Represents a networked zombie entity in the game world, handling synchronization of zombie state
/// across connected clients including health, position, and follower relationships.
/// </summary>
internal sealed class ZombieNetworked : NetworkClass
{
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

    public void OnDestroy()
    {
        _Zombie?.RemoveNetworkedLookup();
        if (_Zombie?.mDead == false)
        {
            _Zombie?.DieDeserialize();
        }
    }

    internal bool EnteringHouse;
    private float _syncCooldown = 2f;
    private float _lastPos;

    public void Update()
    {
        if (!IsOnNetwork) return;

        if (AmOwner)
        {
            if (_Zombie != null)
            {
                if (!_Zombie.mDead)
                {
                    if (_Zombie.mZombieType is not (ZombieType.Gravestone or ZombieType.Bungee))
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
            }
        }
        else
        {
            if (!EnteringHouse)
            {
                if (_Zombie?.mPosX <= 0f)
                {
                    _Zombie?.mPosX = 0f;
                }
            }
        }
    }

    [HideFromIl2Cpp]
    public override void HandleRpc(SteamNetClient sender, byte rpcId, PacketReader packetReader)
    {
        if (sender.SteamId != OwnerId) return;

        switch (rpcId)
        {
            case 0:
                {
                    var theDamage = packetReader.ReadInt();
                    var damageFlags = (DamageFlags)packetReader.ReadByte();
                    HandleTakeDamageRpc(theDamage, damageFlags);
                }
                break;
            case 1:
                {
                    var damageFlags = (DamageFlags)packetReader.ReadByte();
                    HandleDeathRpc(damageFlags);
                }
                break;
            case 2:
                {
                    var xPos = packetReader.ReadFloat();
                    HandleEnteringHouseRpc(xPos);
                }
                break;
        }
    }

    internal void SendTakeDamageRpc(int theDamage, DamageFlags theDamageFlags)
    {
        var writer = PacketWriter.Get();
        writer.WriteInt(theDamage);
        writer.WriteByte((byte)theDamageFlags);
        this.SendRpc(0, writer);
        writer.Recycle();
    }

    [HideFromIl2Cpp]
    private void HandleTakeDamageRpc(int theDamage, DamageFlags damageFlags)
    {
        // Only die from rpc
        if (((_Zombie.mBodyHealth + _Zombie.mHelmHealth + _Zombie.mShieldHealth) - theDamage) > 1)
        {
            _Zombie.TakeDamageOriginal(theDamage, damageFlags);
        }
    }

    private bool dead;
    internal void SendDeathRpc(DamageFlags damageFlags)
    {
        if (!dead)
        {
            dead = true;
            var writer = PacketWriter.Get();
            writer.WriteByte((byte)damageFlags);
            this.SendRpc(1, writer);
            writer.Recycle();
            Despawn();
            Destroy(gameObject);
        }
    }

    [HideFromIl2Cpp]
    private void HandleDeathRpc(DamageFlags damageFlags)
    {
        if (!dead)
        {
            dead = true;
            CheckDeath(() =>
            {
                _Zombie.PlayDeathAnimOriginal(damageFlags);
            }, true);
        }
    }

    internal void SendEnteringHouseRpc(float xPos)
    {
        EnteringHouse = true;
        var writer = PacketWriter.Get();
        writer.WriteFloat(xPos);
        this.SendRpc(2, writer);
        writer.Recycle();
    }

    [HideFromIl2Cpp]
    internal void HandleEnteringHouseRpc(float xPos)
    {
        EnteringHouse = true;
        StopLarpPos();
        _Zombie?.mPosX = xPos;
        VersusManager.EndGame(_Zombie?.mController?.gameObject, PlayerTeam.Zombies);
    }

    [HideFromIl2Cpp]
    internal void CheckDeath(Action callback, bool isRpc = false)
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
            if (isRpc)
            {
                Instances.GameplayActivity.VersusMode.ZombieLife--;
            }

            if (Instances.GameplayActivity.VersusMode.ZombieLife > 0)
            {
                callback();
            }
            else
            {
                VersusManager.EndGame(_Zombie?.mController?.gameObject, PlayerTeam.Plants);
                callback();
            }
        }
        else
        {
            callback();
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

            return;
        }

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

            gameObject.name = $"{Enum.GetName(_Zombie.mZombieType)}_Zombie ({NetworkId})";

            return;
        }

        if (!AmOwner)
        {
            _Zombie.mVelX = packetReader.ReadFloat();
            _Zombie.UpdateAnimSpeed();
            var posX = packetReader.ReadFloat();
            LarpPos(posX);
        }
    }

    /// <summary>
    /// Token used to track and manage position interpolation coroutines.
    /// </summary>
    private object larpToken;

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

            if (distance < 100f)
            {
                larpToken = MelonCoroutines.Start(CoLarpPos(posX));
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
        if (larpToken != null)
        {
            MelonCoroutines.Stop(larpToken);
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

        larpToken = null;
    }

    private static float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Network.Client.Object.Component;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Utilities;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Client.Object.Replanted.ZombieComponents;

/// <inheritdoc/>
internal class ZombieNetworkComponent : NetworkComponent
{
    protected ZombieNetworked ZombieNetworked;

    internal static ZombieNetworkComponent AddComponent(ZombieNetworked zombieNetworked, ZombieType zombieType)
    {
        return zombieType switch
        {
            ZombieType.Polevaulter => zombieNetworked.AddNetworkComponent<PoleVaulterNetworkComponent>(),
            ZombieType.Ladder => zombieNetworked.AddNetworkComponent<LadderNetworkComponent>(),
            ZombieType.JackInTheBox => zombieNetworked.AddNetworkComponent<JackInTheBoxNetworkComponent>(),
            ZombieType.Digger => zombieNetworked.AddNetworkComponent<DiggerNetworkComponent>(),
            ZombieType.Bungee => zombieNetworked.AddNetworkComponent<BungeeNetworkComponent>(),
            ZombieType.Bobsled => zombieNetworked.AddNetworkComponent<BobsledNetworkComponent>(),
            ZombieType.Imp => zombieNetworked.AddNetworkComponent<ImpNetworkComponent>(),
            _ => zombieNetworked.AddNetworkComponent<ZombieNetworkComponent>(),
        };
    }

    internal override void Init()
    {
        ZombieNetworked = NetworkObject as ZombieNetworked;
    }

    private float _syncCooldown = 2f;
    private float _lastPos;
    internal override void Update()
    {
        UpdatePositionSync();
    }

    protected void UpdatePositionSync()
    {
        if (ZombieNetworked._Zombie == null) return;

        if (ZombieNetworked.AmOwner)
        {
            if (!ZombieNetworked._Zombie.mDead)
            {
                if (_syncCooldown <= 0f && _lastPos != ZombieNetworked._Zombie.mPosX)
                {
                    ZombieNetworked.MarkDirty();
                    _syncCooldown = 2f;
                    _lastPos = ZombieNetworked._Zombie.mPosX;
                }
                _syncCooldown -= Time.deltaTime;
            }
        }
        else
        {
            if (!ZombieNetworked.EnteringHouse)
            {
                if (ZombieNetworked._Zombie.mPosX <= 0f)
                {
                    ZombieNetworked._Zombie.mPosX = 0f;
                }
            }
        }
    }

    internal override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init) return;

        packetWriter.WriteInt(ZombieNetworked._Zombie.mRow);
        packetWriter.WriteFloat(ZombieNetworked._Zombie.mVelX);
        packetWriter.WriteFloat(ZombieNetworked._Zombie.mPosX);
    }

    internal override void Deserialize(PacketReader packetReader, bool init)
    {
        if (init) return;

        if (!ZombieNetworked.AmOwner)
        {
            ZombieNetworked._Zombie.mRow = packetReader.ReadInt();
            ZombieNetworked._Zombie.mVelX = packetReader.ReadFloat();
            ZombieNetworked._Zombie.UpdateAnimSpeed();
            var posX = packetReader.ReadFloat();
            LastSyncPosX = posX;
            LarpPos(posX);
        }
    }

    private Coroutine _larpCoroutine;
    internal float? LastSyncPosX;

    /// <summary>
    /// Smoothly interpolates the zombie's position to the target position when distance threshold is exceeded.
    /// </summary>
    /// <param name="posX">The target X position to interpolate to</param>
    private void LarpPos(float posX)
    {
        if (ZombieNetworked._Zombie == null || ZombieNetworked.EnteringHouse || posX < 15f) return;

        float currentX = ZombieNetworked._Zombie.mPosX;
        float distance = Mathf.Abs(currentX - posX);

        // Calculate threshold based on velocity (0.5 seconds of movement)
        float threshold = Mathf.Abs(ZombieNetworked._Zombie.mVelX) * 0.3f;
        threshold = Mathf.Clamp(threshold, 10f, 50f);

        if (distance > threshold)
        {
            // Stop existing interpolation
            StopLarpPos();

            if (distance < 100f && ZombieNetworked._Zombie.mZombieType != ZombieType.Pogo)
            {
                _larpCoroutine = ZombieNetworked.StartCoroutine(CoLarpPos(posX));
            }
            else
            {
                ZombieNetworked._Zombie.mPosX = posX;
            }
        }
    }

    /// <summary>
    /// Stop larping to network pos
    /// </summary>
    internal void StopLarpPos()
    {
        if (_larpCoroutine != null)
        {
            LastSyncPosX = null;
            ZombieNetworked.StopCoroutine(_larpCoroutine);
        }
    }

    /// <summary>
    /// Coroutine that smoothly interpolates the zombie's position over time.
    /// </summary>
    /// <param name="targetX">The target X position to reach</param>
    [HideFromIl2Cpp]
    private IEnumerator CoLarpPos(float targetX)
    {
        if (this == null || ZombieNetworked._Zombie == null) yield break;

        float startX = ZombieNetworked._Zombie.mPosX;
        float distance = Mathf.Abs(targetX - startX);

        // Use zombie's current velocity for interpolation speed
        float speed = Mathf.Abs(ZombieNetworked._Zombie.mVelX);
        speed = Mathf.Clamp(speed, 10f, 40f);

        float duration = Mathf.Clamp(distance / speed, 0.1f, 2f);

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            if (this == null || ZombieNetworked._Zombie == null) yield break;

            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            t = SmoothStep(t);

            ZombieNetworked._Zombie.mPosX = Mathf.Lerp(startX, targetX, t);
            yield return null;
        }

        // Ensure final position is exact
        ZombieNetworked._Zombie?.mPosX = targetX;

        LastSyncPosX = null;
        _larpCoroutine = null;
    }

    private static float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }
}

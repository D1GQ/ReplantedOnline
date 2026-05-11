using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Network.Client.Object.Component;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Utilities.Unity;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Client.Object.Reloaded.Components;

/// <inheritdoc/>
internal class ZombieNetworkComponent : NetworkComponent
{
    /// <summary>
    /// Gets the NetworkObject that owns this component.
    /// </summary>
    protected ZombieNetworked Net { get; private set; }

    internal sealed override void Init()
    {
        Net = NetObj as ZombieNetworked;
        OnInit();
    }

    internal virtual void OnInit() { }

    internal virtual void OnDeath(DeathReason deathReason) { }

    internal bool PosSyncingPaused;
    private float _syncCooldown = 2f;
    private float _lastPos;
    internal override void Update()
    {
        UpdatePositionSync();
    }

    protected void UpdatePositionSync()
    {
        if (Net._Zombie == null) return;
        if (PosSyncingPaused) return;

        if (Net.AmOwner)
        {
            if (!Net._Zombie.mDead)
            {
                if (_syncCooldown <= 0f && _lastPos != Net._Zombie.mPosX)
                {
                    Net.MarkDirty();
                    _syncCooldown = 2f;
                    _lastPos = Net._Zombie.mPosX;
                }
                _syncCooldown -= Time.deltaTime;
            }
        }
        else
        {
            if (!Net.EnteringHouse)
            {
                if (Net._Zombie.mPosX <= 0f)
                {
                    Net._Zombie.mPosX = 0f;
                }
            }
        }
    }

    internal override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init) return;

        packetWriter.WriteInt(Net._Zombie.mRow);
        packetWriter.WriteFloat(Net._Zombie.mVelX);
        packetWriter.WriteFloat(Net._Zombie.mPosX);
    }

    internal override void Deserialize(PacketReader packetReader, bool init)
    {
        if (init) return;

        if (!Net.AmOwner)
        {
            Net._Zombie.mRow = packetReader.ReadInt();
            Net._Zombie.mVelX = packetReader.ReadFloat();
            Net._Zombie.UpdateAnimSpeed();
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
        if (PosSyncingPaused) return;
        if (Net._Zombie == null || Net.EnteringHouse || posX < 15f) return;
        if (Net._Zombie.mIceTrapCounter > 0) return;

        float currentX = Net._Zombie.mPosX;
        float distance = Mathf.Abs(currentX - posX);

        // Calculate threshold based on velocity (0.5 seconds of movement)
        float threshold = Mathf.Abs(Net._Zombie.mVelX) * 0.3f;
        threshold = Mathf.Clamp(threshold, 10f, 50f);

        if (distance > threshold)
        {
            // Stop existing interpolation
            StopLarpPos();

            if (distance < 100f && Net._Zombie.mZombieType != ZombieType.Pogo)
            {
                _larpCoroutine = Net.StartCoroutine(CoLarpPos(posX));
            }
            else
            {
                Net._Zombie.mPosX = posX;
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
            _syncCooldown = 2f;
            LastSyncPosX = null;
            Net.StopCoroutine(_larpCoroutine);
        }
    }

    /// <summary>
    /// Coroutine that smoothly interpolates the zombie's position over time.
    /// </summary>
    /// <param name="targetX">The target X position to reach</param>
    [HideFromIl2Cpp]
    private IEnumerator CoLarpPos(float targetX)
    {
        if (this == null || Net._Zombie == null) yield break;

        float startX = Net._Zombie.mPosX;
        float distance = Mathf.Abs(targetX - startX);

        // Use zombie's current velocity for interpolation speed
        float speed = Mathf.Abs(Net._Zombie.mVelX);
        speed = Mathf.Clamp(speed, 10f, 40f);

        float duration = Mathf.Clamp(distance / speed, 0.1f, 2f);

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            if (this == null || Net._Zombie == null) yield break;

            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            t = SmoothStep(t);

            Net._Zombie.mPosX = Mathf.Lerp(startX, targetX, t);
            yield return null;
        }

        // Ensure final position is exact
        Net._Zombie?.mPosX = targetX;

        LastSyncPosX = null;
        _larpCoroutine = null;
    }

    private static float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }
}

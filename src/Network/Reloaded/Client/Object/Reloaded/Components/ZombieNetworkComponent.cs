using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Network.Reloaded.Client.Object.Component;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Utilities.Unity;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Reloaded.Components;

/// <inheritdoc/>
internal class ZombieNetworkComponent : NetworkComponent
{
    /// <summary>
    /// Gets the NetworkObject that owns this component.
    /// </summary>
    protected ZombieNetworked Net { get; private set; } = default!;

    internal sealed override void Init()
    {
        Net = (NetObj as ZombieNetworked)!;
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
        if (Net.Zombie == null) return;
        if (PosSyncingPaused) return;

        if (Net.AmOwner)
        {
            if (!Net.Zombie.mDead)
            {
                if (_syncCooldown <= 0f && _lastPos != Net.Zombie.mPosX)
                {
                    Net.MarkDirty();
                    _syncCooldown = 2f;
                    _lastPos = Net.Zombie.mPosX;
                }
                _syncCooldown -= Time.deltaTime;
            }
        }
        else
        {
            if (!Net.EnteringHouse)
            {
                if (Net.Zombie.mPosX <= 0f)
                {
                    Net.Zombie.mPosX = 0f;
                }
            }
        }
    }

    public override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init) return;

        packetWriter.WriteBool(Net.Zombie == null);
        if (Net.Zombie != null)
        {
            packetWriter.WritePackedInt(Net.Zombie.mRow);
            packetWriter.WriteFloat(Net.Zombie.mVelX);
            packetWriter.WriteFloat(Net.Zombie.mPosX);
        }
    }

    public override void Deserialize(PacketReader packetReader, bool init)
    {
        if (init) return;

        if (!Net.AmOwner)
        {
            bool isZombieNull = packetReader.ReadBool();
            if (!isZombieNull && Net.Zombie != null)
            {
                Net.Zombie.mRow = packetReader.ReadPackedInt();
                Net.Zombie.mVelX = packetReader.ReadFloat();
                Net.Zombie.UpdateAnimSpeed();
                var posX = packetReader.ReadFloat();
                LastSyncPosX = posX;
                LarpPos(posX);
            }
        }
    }

    private Coroutine? _larpCoroutine;
    internal float? LastSyncPosX;

    /// <summary>
    /// Smoothly interpolates the zombie's position to the target position when distance threshold is exceeded.
    /// </summary>
    /// <param name="posX">The target X position to interpolate to</param>
    private void LarpPos(float posX)
    {
        if (PosSyncingPaused) return;
        if (Net.Zombie == null || Net.EnteringHouse || posX < 15f) return;
        if (Net.Zombie.mIceTrapCounter > 0) return;

        float currentX = Net.Zombie.mPosX;
        float distance = Mathf.Abs(currentX - posX);

        // Calculate threshold based on velocity (0.5 seconds of movement)
        float threshold = Mathf.Abs(Net.Zombie.mVelX) * 0.3f;
        threshold = Mathf.Clamp(threshold, 10f, 50f);

        if (distance > threshold)
        {
            // Stop existing interpolation
            StopLarpPos();

            if (distance < 100f && Net.Zombie.mZombieType != ZombieType.Pogo)
            {
                _larpCoroutine = Net.StartCoroutine(CoLarpPos(posX));
            }
            else
            {
                Net.Zombie.mPosX = posX;
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
        if (this == null || Net.Zombie == null) yield break;

        float startX = Net.Zombie.mPosX;
        float distance = Mathf.Abs(targetX - startX);

        // Use zombie's current velocity for interpolation speed
        float speed = Mathf.Abs(Net.Zombie.mVelX);
        speed = Mathf.Clamp(speed, 10f, 40f);

        float duration = Mathf.Clamp(distance / speed, 0.1f, 2f);

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            if (this == null || Net.Zombie == null) yield break;

            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            t = SmoothStep(t);

            Net.Zombie.mPosX = Mathf.Lerp(startX, targetX, t);
            yield return null;
        }

        // Ensure final position is exact
        Net.Zombie?.mPosX = targetX;

        LastSyncPosX = null;
        _larpCoroutine = null;
    }

    private static float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }
}

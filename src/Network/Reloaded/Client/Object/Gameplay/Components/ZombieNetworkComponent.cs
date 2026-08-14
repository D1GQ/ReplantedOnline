using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Reloaded.Versus;
using ReplantedOnline.Modules.Unity;
using ReplantedOnline.Network.Reloaded.Client.Object.Component;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Patches.Reloaded.Gameplay.Versus.Networked;
using ReplantedOnline.Utilities.Modded;
using UnityEngine;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

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
        var zombie = Net.Zombie;
        if (zombie != null)
        {
            OnInit(zombie);
        }
    }

    internal virtual void OnInit(Zombie zombie) { }

    private static readonly ZombieType[] NonGroaningZombies =
    [
        ZombieType.Gravestone,
        ZombieType.Target,
        ZombieType.Yeti
    ];

    private bool _hasPicked = false;
    private bool _pickingSpeed;
    private float _previousVelX = -1;
    private float _velX;

    internal void PickRandomSpeed(Zombie zombie)
    {
        _pickingSpeed = true;
        _previousVelX = -1;
        zombie.PickRandomSpeedOriginal();
        _velX = zombie.mVelX;
        _pickingSpeed = false;
    }

    internal void SetSpeed(float velX)
    {
        if (Net.Zombie == null)
            return;

        _pickingSpeed = true;
        _previousVelX = -1;
        _velX = velX;
        _pickingSpeed = false;
    }

    internal float GetSpeedBuffMultiplier(Zombie zombie)
    {
        float multiplier = 1f;

        if (zombie.mZombieType is not (ZombieType.Dancer or ZombieType.BackupDancer))
        {
            foreach (var zom in zombie.mBoard.GetZombies())
            {
                float distance = Vector2.Distance(new(zombie.mPosX, zombie.mPosY), new(zom.mPosX, zom.mPosY));

                if (distance < 225)
                {
                    if (zom.mZombieType == ZombieType.Dancer)
                    {
                        multiplier += 0.1f * Mathf.Lerp(3.5f, 1f, distance / 255);
                    }
                    else if (zom.mZombieType == ZombieType.BackupDancer)
                    {
                        multiplier += 0.05f * Mathf.Lerp(3.5f, 1f, distance / 255);
                    }
                }
            }
        }

        if (zombie.mZombieType is not (ZombieType.DolphinRider or ZombieType.Snorkel))
        {
            if (Net.PoolComponent.InPool)
            {
                multiplier *= 0.95f;
            }
        }

        if (VersusState.ArenaSynced == ArenaType.PoolNight)
        {
            var gridX = PvZRUtils.ReloadedObjectXToGridX(zombie.mX);
            if (gridX >= zombie.mBoard.LeftFogColumn() &&
                FogUtils.GetFogAt(zombie.mBoard, gridX, zombie.mRow + 1) > 100)
            {
                multiplier *= 0.9f;
            }
        }

        return multiplier;
    }

    private float _accumulatedDistance = 0f;
    private readonly object _distanceLock = new();

    internal void AddDistance(float distance)
    {
        lock (_distanceLock)
        {
            _accumulatedDistance += distance;
        }
    }

    internal sealed override void Update()
    {
        var zombie = Net.Zombie;
        if (zombie == null)
            return;

        if (NonGroaningZombies.Contains(zombie.mZombieType))
        {
            zombie.mGroanCounter = int.MaxValue;
        }

        OnUpdate(zombie);

        if (!_hasPicked && Net.AmOwner)
        {
            if (zombie.mZombiePhase != ZombiePhase.RisingFromGrave)
            {
                _hasPicked = true;
                PickRandomSpeed(zombie);
            }
        }

        if (!_pickingSpeed)
        {
            var trueVelocity = _velX * GetSpeedBuffMultiplier(zombie);
            if (_previousVelX != trueVelocity)
            {
                _previousVelX = trueVelocity;
                zombie.mVelX = trueVelocity;
                zombie.UpdateAnimSpeed();
            }
        }

        float distance;
        lock (_distanceLock)
        {
            distance = _accumulatedDistance;
            _accumulatedDistance = 0f;
        }
        UpdatePosition(zombie, distance);
    }

    internal virtual void OnUpdate(Zombie zombie) { }

    internal virtual void OnDeath(Zombie? zombie, DeathReason deathReason) { }

    private readonly UnityTimer dirtyPosTimer = new();
    internal float? SyncedPosX;

    protected virtual void UpdatePosition(Zombie zombie, float distance, bool useNonNetworkLogic = false)
    {
        // Don't allow position updates during PushBack event
        if (Net.Event == EventState.PushBack)
        {
            return;
        }

        if (useNonNetworkLogic)
        {
            // Move the zombie based on walking direction
            if (!zombie.IsWalkingBackwards())
            {
                zombie.mPosX -= distance;
            }
            else
            {
                zombie.mPosX += distance;
            }

            return;
        }

        if (Net.AmOwner)
        {
            UpdatePosition(zombie, distance, true);

            // Sync position to network every 0.25 seconds, but only if position changed
            if (dirtyPosTimer.AccumulatedTime > 0.25f &&
                SyncedPosX != zombie.mPosX)
            {
                SyncedPosX = zombie.mPosX;
                dirtyPosTimer.Reset();
                Net.MarkDirty();
            }
        }
        else
        {
            if (SyncedPosX == null)
                return;

            // Calculate the difference between current and target positions
            float targetPos = SyncedPosX.Value;
            float currentPos = zombie.mPosX;
            float diff = targetPos - currentPos;

            if (Mathf.Abs(diff) < 0.001f)
            {
                zombie.mPosX = targetPos;
                SyncedPosX = null;
                return;
            }

            // Get absolute distance
            float diffAbs = Mathf.Abs(diff);

            // Speed multiplier
            float speedMultiplier = 0.8f + (diffAbs * 0.02f);

            // Cap max speed
            speedMultiplier = Mathf.Min(speedMultiplier, 5f);

            // Calculate how much to move:
            float moveAmount = Mathf.Min(diffAbs, distance * speedMultiplier);

            // Determine direction to move
            float moveDirection = Mathf.Sign(diff);

            // Apply the movement
            zombie.mPosX += moveAmount * moveDirection;
        }
    }

    /// <summary>
    /// Smoothly interpolates the zombie's position toward the network-synced target.
    /// </summary>
    internal void InterpolatePosition()
    {
        if (Net.AmOwner)
            return;

        if (SyncedPosX == null)
            return;

        var zombie = Net.Zombie;
        if (zombie == null)
            return;

        UpdatePosition(zombie, 1f);
    }

    public override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init)
            return;

        packetWriter.WriteBool(Net.Zombie == null);
        if (Net.Zombie != null)
        {
            packetWriter.WritePackedFloat(_velX, 500f);
            packetWriter.WritePackedFloat(Net.Zombie.mPosX, 25f);
        }
    }

    public override void Deserialize(PacketReader packetReader, bool init)
    {
        if (init)
            return;

        if (!Net.AmOwner)
        {
            bool isZombieNull = packetReader.ReadBool();
            if (!isZombieNull && Net.Zombie != null)
            {
                SetSpeed(packetReader.ReadPackedFloat(500f));
                SyncedPosX = packetReader.ReadPackedFloat(25f);
            }
        }
    }
}

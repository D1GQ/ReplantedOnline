using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Unity;
using ReplantedOnline.Network.Reloaded.Client.Object.Component;
using ReplantedOnline.Network.Reloaded.Serialization;
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
        OnInit();
    }

    internal virtual void OnInit() { }

    internal virtual void OnDeath(DeathReason deathReason) { }

    private readonly UnityTimer dirtyPosTimer = new();
    private float? syncedPosX;

    /// <summary>
    /// Updates the zombie's position.
    /// </summary>
    /// <param name="distance">The base distance to move per update</param>
    /// <param name="useNonNetworkLogic">Use the base update position logic</param>
    internal virtual void UpdatePosition(float distance, bool useNonNetworkLogic = false)
    {
        if (Net.Zombie == null)
            return;

        // Don't allow position updates during PushBack event
        if (Net.Event == EventState.PushBack)
        {
            return;
        }

        if (useNonNetworkLogic)
        {
            // Move the zombie based on walking direction
            if (!Net.Zombie.IsWalkingBackwards())
            {
                Net.Zombie.mPosX -= distance;
            }
            else
            {
                Net.Zombie.mPosX += distance;
            }

            return;
        }

        if (Net.AmOwner)
        {
            UpdatePosition(distance, true);

            // Sync position to network every 0.25 seconds, but only if position changed
            if (dirtyPosTimer.AccumulatedTime > 0.25f &&
                syncedPosX != Net.Zombie.mPosX)
            {
                syncedPosX = Net.Zombie.mPosX;
                dirtyPosTimer.Reset();
                Net.MarkDirty();
            }
        }
        else
        {
            if (syncedPosX == null)
                return;

            // Calculate the difference between current and target positions
            float targetPos = syncedPosX.Value;
            float currentPos = Net.Zombie.mPosX;
            float diff = targetPos - currentPos;

            if (Mathf.Abs(diff) < 0.001f)
            {
                Net.Zombie.mPosX = targetPos;
                syncedPosX = null;
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
            Net.Zombie.mPosX += moveAmount * moveDirection;
        }
    }

    /// <summary>
    /// Smoothly interpolates the zombie's position toward the network-synced target.
    /// </summary>
    internal void InterpolatePosition()
    {
        if (Net.Zombie == null)
            return;

        if (Net.AmOwner)
            return;

        if (syncedPosX == null)
            return;

        UpdatePosition(1f);
    }

    public override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init) return;

        packetWriter.WriteBool(Net.Zombie == null);
        if (Net.Zombie != null)
        {
            packetWriter.WriteFloat(Net.Zombie.mVelX);
            short packedPos = (short)(Net.Zombie.mPosX * 25f);
            packetWriter.WriteShort(packedPos);
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
                Net.Zombie.mVelX = packetReader.ReadFloat();
                Net.Zombie.UpdateAnimSpeed();
                short packedPos = packetReader.ReadShort();

                if (packedPos == short.MaxValue || packedPos == short.MinValue)
                {
                    syncedPosX = packedPos > 0 ? 1310.68f : -1310.72f;
                }
                else
                {
                    syncedPosX = packedPos / 25f;
                }
            }
        }
    }
}

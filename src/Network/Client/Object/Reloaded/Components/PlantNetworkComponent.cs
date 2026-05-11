using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Network.Client.Object.Component;
using ReplantedOnline.Network.Packet;
using UnityEngine;

namespace ReplantedOnline.Network.Client.Object.Reloaded.Components;

/// <inheritdoc/>
internal class PlantNetworkComponent : NetworkComponent
{
    /// <summary>
    /// Gets the NetworkObject that owns this component.
    /// </summary>
    protected PlantNetworked Net { get; private set; }

    internal sealed override void Init()
    {
        Net = NetObj as PlantNetworked;
        OnInit();
    }

    internal virtual void OnInit() { }

    internal virtual void OnDeath(DeathReason deathReason) { }

    internal int? lastSyncPlantHealth;
    private float _syncHealthCooldown = 2f;
    internal override void Update()
    {
        UpdateHealthSync();
    }

    protected void UpdateHealthSync()
    {
        if (Net._Plant == null) return;

        if (Net.AmOwner)
        {
            if (!Net.Dead && !Net._Plant.mDead)
            {
                if (_syncHealthCooldown <= 0f && lastSyncPlantHealth != Net._Plant.mPlantHealth)
                {
                    Net.MarkDirty();
                    _syncHealthCooldown = 1f;
                    lastSyncPlantHealth = Net._Plant.mPlantHealth;
                }
                _syncHealthCooldown -= Time.deltaTime;
            }
        }
        else
        {
            if (!Net.Dead && !Net._Plant.mDead)
            {
                if (lastSyncPlantHealth != null)
                {
                    Net._Plant.mPlantHealth = lastSyncPlantHealth.Value;
                }

                if (Net._Plant.mPlantHealth < 25)
                {
                    Net._Plant.mPlantHealth = 25;
                }
            }
        }
    }

    internal override void Serialize(PacketWriter packetWriter, bool init)
    {
        packetWriter.WriteInt(Net._Plant.mPlantHealth);
    }

    internal override void Deserialize(PacketReader packetReader, bool init)
    {
        lastSyncPlantHealth = Math.Max(packetReader.ReadInt(), 25);
    }
}
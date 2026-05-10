using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Network.Client.Object.Component;
using ReplantedOnline.Network.Packet;
using UnityEngine;

namespace ReplantedOnline.Network.Client.Object.Reloaded.Components;

/// <inheritdoc/>
internal class PlantNetworkComponent : NetworkComponent
{
    protected PlantNetworked PlantNetworked;

    internal sealed override void Init()
    {
        PlantNetworked = NetworkObject as PlantNetworked;
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
        if (PlantNetworked._Plant == null) return;

        if (PlantNetworked.AmOwner)
        {
            if (!PlantNetworked.Dead && !PlantNetworked._Plant.mDead)
            {
                if (_syncHealthCooldown <= 0f && lastSyncPlantHealth != PlantNetworked._Plant.mPlantHealth)
                {
                    PlantNetworked.MarkDirty();
                    _syncHealthCooldown = 1f;
                    lastSyncPlantHealth = PlantNetworked._Plant.mPlantHealth;
                }
                _syncHealthCooldown -= Time.deltaTime;
            }
        }
        else
        {
            if (!PlantNetworked.Dead && !PlantNetworked._Plant.mDead)
            {
                if (lastSyncPlantHealth != null)
                {
                    PlantNetworked._Plant.mPlantHealth = lastSyncPlantHealth.Value;
                }

                if (PlantNetworked._Plant.mPlantHealth < 25)
                {
                    PlantNetworked._Plant.mPlantHealth = 25;
                }
            }
        }
    }

    internal override void Serialize(PacketWriter packetWriter, bool init)
    {
        packetWriter.WriteInt(PlantNetworked._Plant.mPlantHealth);
    }

    internal override void Deserialize(PacketReader packetReader, bool init)
    {
        lastSyncPlantHealth = Math.Max(packetReader.ReadInt(), 25);
    }
}
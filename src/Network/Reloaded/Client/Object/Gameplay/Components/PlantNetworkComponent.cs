using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Network.Reloaded.Client.Object.Component;
using ReplantedOnline.Network.Reloaded.Serialization;
using UnityEngine;

namespace ReplantedOnline.Network.Reloaded.Client.Object.Gameplay.Components;

/// <inheritdoc/>
internal class PlantNetworkComponent : NetworkComponent
{
    /// <summary>
    /// Gets the NetworkObject that owns this component.
    /// </summary>
    protected PlantNetworked Net { get; private set; } = default!;

    internal sealed override void Init()
    {
        Net = (NetObj as PlantNetworked)!;
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
        if (Net.Plant == null) return;

        if (Net.AmOwner)
        {
            if (!Net.Dead && !Net.Plant.mDead)
            {
                if (_syncHealthCooldown <= 0f && lastSyncPlantHealth != Net.Plant.mPlantHealth)
                {
                    Net.MarkDirty();
                    _syncHealthCooldown = 1f;
                    lastSyncPlantHealth = Net.Plant.mPlantHealth;
                }
                _syncHealthCooldown -= Time.deltaTime;
            }
        }
        else
        {
            if (!Net.Dead && !Net.Plant.mDead)
            {
                if (lastSyncPlantHealth != null)
                {
                    Net.Plant.mPlantHealth = lastSyncPlantHealth.Value;
                }

                if (Net.Plant.mPlantHealth < 25)
                {
                    Net.Plant.mPlantHealth = 25;
                }
            }
        }
    }

    public override void Serialize(PacketWriter packetWriter, bool init)
    {
        packetWriter.WritePackedInt(Mathf.Max(Net.Plant?.mPlantHealth ?? 25, 0));
    }

    public override void Deserialize(PacketReader packetReader, bool init)
    {
        lastSyncPlantHealth = Math.Max(packetReader.ReadPackedInt(), 25);
    }
}
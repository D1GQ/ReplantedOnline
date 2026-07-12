using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Unity;
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

    internal override void Update()
    {
        UpdateHealthSync();
    }

    internal int? lastSyncPlantHealth;
    private readonly UnityTimer _dirtyHpTimer = new();
    protected void UpdateHealthSync()
    {
        if (Net.Plant == null) return;

        if (Net.AmOwner)
        {
            if (!Net.Dying && !Net.Plant.mDead)
            {
                if (_dirtyHpTimer.AccumulatedTime > 1f && lastSyncPlantHealth != Net.Plant.mPlantHealth)
                {
                    _dirtyHpTimer.Reset();
                    Net.MarkDirty();
                    lastSyncPlantHealth = Net.Plant.mPlantHealth;
                }
            }
        }
        else
        {
            if (!Net.Dying && !Net.Plant.mDead)
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
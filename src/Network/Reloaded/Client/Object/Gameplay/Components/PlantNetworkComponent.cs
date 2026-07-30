using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Unity;
using ReplantedOnline.Network.Reloaded.Client.Object.Component;
using ReplantedOnline.Network.Reloaded.Serialization;

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

    internal virtual void OnDeath(Plant? plant, DeathReason deathReason) { }

    internal sealed override void Update()
    {
        var plant = Net.Plant;
        if (plant == null)
            return;

        OnUpdate(plant);
    }

    internal virtual void OnUpdate(Plant plant)
    {
        UpdateHealthSync();
    }

    internal int? lastSyncPlantHealth;
    private readonly UnityTimer _dirtyHpTimer = new();
    protected void UpdateHealthSync()
    {
        if (Net.Plant == null)
            return;

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
        packetWriter.WriteBool(Net.Plant == null);
        if (Net.Plant != null)
        {
            packetWriter.WritePackedInt(Net.Plant.mPlantHealth);
        }
    }

    public override void Deserialize(PacketReader packetReader, bool init)
    {
        bool isPlantNull = packetReader.ReadBool();
        if (!isPlantNull && Net.Plant != null)
        {
            lastSyncPlantHealth = Math.Max(packetReader.ReadPackedInt(), 25);
        }
    }
}
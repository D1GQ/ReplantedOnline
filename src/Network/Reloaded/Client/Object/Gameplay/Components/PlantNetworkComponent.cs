using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Unity;
using ReplantedOnline.Network.Reloaded.Client.Object.Component;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Utilities.Modded;

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
        var plant = Net.Plant;
        if (plant != null)
        {
            OnInit(plant);
        }
    }

    internal virtual void OnInit(Plant plant) { }

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
        UpdateHealthSync(plant);
    }

    internal int? lastSyncPlantHealth;
    private readonly UnityTimer _dirtyHpTimer = new();
    protected void UpdateHealthSync(Plant plant)
    {
        if (Net.AmOwner)
        {
            if (!Net.Dying && !plant.mDead)
            {
                if (_dirtyHpTimer.AccumulatedTime > 1f && lastSyncPlantHealth != plant.mPlantHealth)
                {
                    _dirtyHpTimer.Reset();
                    Net.MarkDirty();
                    lastSyncPlantHealth = plant.mPlantHealth;
                }
            }
        }
        else
        {
            if (!Net.Dying && !plant.mDead)
            {
                if (lastSyncPlantHealth != null)
                {
                    plant.mPlantHealth = lastSyncPlantHealth.Value;
                }

                if (plant.mPlantHealth < 25)
                {
                    plant.mPlantHealth = 25;
                }
            }
        }
    }

    public override void Serialize(PacketWriter packetWriter, bool init)
    {
        var plant = Net.Plant;
        packetWriter.WriteBool(Net.Plant == null);

        if (init)
        {
            if (plant != null)
            {
                plant.mController.SerializeCharacterSkin(packetWriter);
            }
        }

        if (plant != null)
        {
            packetWriter.WritePackedInt(plant.mPlantHealth);
        }
    }

    public override void Deserialize(PacketReader packetReader, bool init)
    {
        var plant = Net.Plant;
        bool isPlantNull = packetReader.ReadBool();

        if (init)
        {
            if (!isPlantNull && plant != null)
            {
                plant.mController.DeserializeCharacterSkin(packetReader);
            }
        }

        if (!isPlantNull && plant != null)
        {
            lastSyncPlantHealth = Math.Max(packetReader.ReadPackedInt(), 25);
        }
    }
}
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Network.Client.Object.Component;
using ReplantedOnline.Network.Packet;
using UnityEngine;

namespace ReplantedOnline.Network.Client.Object.Replanted.PlantComponents;

/// <inheritdoc/>
internal class PlantNetworkComponent : NetworkComponent
{
    protected PlantNetworked PlantNetworked;

    internal static PlantNetworkComponent AddComponent(PlantNetworked plantNetworked, SeedType seedType)
    {
        return seedType switch
        {
            SeedType.Squash => plantNetworked.AddNetworkComponent<SquashNetworkComponent>(),
            SeedType.Chomper => plantNetworked.AddNetworkComponent<ChomperNetworkComponent>(),
            SeedType.Magnetshroom => plantNetworked.AddNetworkComponent<MagnetShroomNetworkComponent>(),
            _ => plantNetworked.AddNetworkComponent<PlantNetworkComponent>(),
        };
    }

    internal override void Init()
    {
        PlantNetworked = NetworkObject as PlantNetworked;
    }

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
            }
        }
    }

    internal override void Serialize(PacketWriter packetWriter, bool init)
    {
        packetWriter.WriteInt(PlantNetworked._Plant.mPlantHealth);
    }

    internal override void Deserialize(PacketReader packetReader, bool init)
    {
        lastSyncPlantHealth = Math.Max(packetReader.ReadInt(), 5);
    }
}
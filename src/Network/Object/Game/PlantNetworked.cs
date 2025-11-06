using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Helper;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Versus.NetworkSync;

namespace ReplantedOnline.Network.Object.Game;

/// <summary>
/// Represents a networked plant entity in the game world, handling synchronization of plant state
/// across connected clients including plant type, position, and imitater type.
/// </summary>
internal sealed class PlantNetworked : NetworkClass
{
    /// <summary>
    /// Dictionary mapping plant instances to their networked counterparts for easy lookup.
    /// </summary>
    internal static Dictionary<Plant, PlantNetworked> NetworkedPlants = [];

    /// <summary>
    /// The underlying plant instance that this networked object represents.
    /// </summary>
    internal Plant _Plant;

    /// <summary>
    /// The type of seed used to plant this plant when spawning.
    /// </summary>
    internal SeedType SeedType;

    /// <summary>
    /// The imitater type if this plant was created by an Imitater seed when spawning.
    /// </summary>
    internal SeedType ImitaterType;

    /// <summary>
    /// The grid X coordinate where this plant is located when spawning.
    /// </summary>
    internal int GridX;

    /// <summary>
    /// The grid Y coordinate where this plant is located when spawning.
    /// </summary>
    internal int GridY;

    public void Update()
    {
        if (AmOwner)
        {
            if (_Plant?.mDead != true)
            {

            }
            else if (!IsDespawning)
            {
                DespawnAndDestroyWithDelay(3f);
            }
        }
        else
        {
            if (!dead)
            {
                _Plant?.mDead = false;
            }
        }
    }

    /// <summary>
    /// Called when the plant is destroyed, cleans up the plant from the networked plants dictionary.
    /// </summary>
    public void OnDestroy()
    {
        if (_Plant != null)
        {
            NetworkedPlants.Remove(_Plant);
        }
    }

    /// <summary>
    /// Handles incoming RPC calls for this plant.
    /// </summary>
    /// <param name="sender">The client that sent the RPC</param>
    /// <param name="rpcId">The identifier of the RPC method</param>
    /// <param name="packetReader">The packet reader containing RPC data</param>
    [HideFromIl2Cpp]
    public override void HandleRpc(SteamNetClient sender, byte rpcId, PacketReader packetReader)
    {
        switch (rpcId)
        {
            case 0:
                HandleDieRpc();
                break;
            case 1:
            HandleTargetZombie(packetReader);
                break;
        }
    }

    internal void SendDieRpc()
    {
        this.SendRpc(0, false);
    }


    private bool dead;
    private void HandleDieRpc()
    {
        dead = true;
        _Plant.DieOriginal();
    }

    [HideFromIl2Cpp]
    internal void SendTargetZombie(ZombieNetworked zombie)
    {
        var packet = PacketWriter.Get();
        packet.WriteNetworkClass(zombie);
        this.SendRpc(1, packet, false);
    }

    [HideFromIl2Cpp]
    private void HandleTargetZombie(PacketReader packetReader)
    {
        var zombieNet = (ZombieNetworked)packetReader.ReadNetworkClass();
        _Plant?.mTargetZombieID = zombieNet._Zombie.DataID;
    }

    /// <summary>
    /// Serializes the plant state for network transmission.
    /// </summary>
    /// <param name="packetWriter">The packet writer to write data to</param>
    /// <param name="init">Whether this is initial synchronization data</param>
    [HideFromIl2Cpp]
    public override void Serialize(PacketWriter packetWriter, bool init)
    {
        if (init)
        {
            // Set spawn info
            packetWriter.WriteInt(GridX);
            packetWriter.WriteInt(GridY);
            packetWriter.WriteInt((int)SeedType);
            packetWriter.WriteInt((int)ImitaterType);
        }
    }

    /// <summary>
    /// Deserializes the plant state from network data and spawns the plant instance.
    /// </summary>
    /// <param name="packetReader">The packet reader to read data from</param>
    /// <param name="init">Whether this is initial synchronization data</param>
    [HideFromIl2Cpp]
    public override void Deserialize(PacketReader packetReader, bool init)
    {
        if (init)
        {
            // Read spawn info
            GridX = packetReader.ReadInt();
            GridY = packetReader.ReadInt();
            SeedType = (SeedType)packetReader.ReadInt();
            ImitaterType = (SeedType)packetReader.ReadInt();

            _Plant = Utils.SpawnPlant(SeedType, ImitaterType, GridX, GridY, false);

            NetworkedPlants[_Plant] = this;
        }
    }
}
using Il2CppInterop.Runtime.Attributes;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Versus.NetworkSync;

namespace ReplantedOnline.Network.Object.Game;

/// <summary>
/// Represents a networked plant entity in the game world, handling synchronization of plant state
/// across connected clients including plant type, position, and imitater type.
/// </summary>
internal class PlantNetworked : NetworkClass
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
    /// The unique identifier for this plant instance when spawning.
    /// </summary>
    internal PlantID PlantID;

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
        // Currently no RPC handlers implemented for plants
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
            packetWriter.WriteInt(PlantID);
            packetWriter.WriteByte((byte)SeedType);
            packetWriter.WriteByte((byte)ImitaterType);
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
            PlantID = packetReader.ReadInt();
            SeedType = (SeedType)packetReader.ReadByte();
            ImitaterType = (SeedType)packetReader.ReadByte();

            _Plant = SeedPacketSyncPatch.SpawnPlant(SeedType, ImitaterType, GridX, GridY, false);
            _Plant.DataID = PlantID;

            NetworkedPlants[_Plant] = this;
        }
    }
}
using Il2CppReloaded.Gameplay;
using Il2CppTekly.PanelViews;
using ReplantedOnline.Enums;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;

namespace ReplantedOnline.Helper;

internal static class Utils
{
    internal static PanelView GetPanel(this PanelViewContainer panelViewContainer, string panelId)
    {
        foreach (var panel in panelViewContainer.m_panels)
        {
            if (panel.Id != panelId) continue;
            return panel;
        }

        return null;
    }

    /// <summary>
    /// Places a seed (plant or zombie) at the specified grid position with network synchronization support
    /// </summary>
    /// <param name="seedType">Type of seed to plant</param>
    /// <param name="imitaterType">Imitater plant type if applicable</param>
    /// <param name="gridX">X grid coordinate (0-8 for plants, 0-8 for zombies)</param>
    /// <param name="gridY">Y grid coordinate (0-4 for lawn rows)</param>
    /// <param name="spawnOnNetwork">Whether to spawn the object on the network for multiplayer synchronization</param>
    /// <returns>The created game object (plant or zombie)</returns>
    internal static ReloadedObject PlaceSeed(SeedType seedType, SeedType imitaterType, int gridX, int gridY, bool spawnOnNetwork)
    {
        return SeedPacketSyncPatch.PlaceSeed(seedType, imitaterType, gridX, gridY, spawnOnNetwork);
    }

    /// <summary>
    /// Spawns a plant at the specified grid position with optional network synchronization
    /// </summary>
    /// <param name="seedType">Type of plant seed to spawn</param>
    /// <param name="imitaterType">Imitater plant type if the plant is mimicking another plant</param>
    /// <param name="gridX">X grid coordinate (0-8)</param>
    /// <param name="gridY">Y grid coordinate (0-4)</param>
    /// <param name="spawnOnNetwork">Whether to create a network controller for multiplayer sync</param>
    /// <returns>The spawned Plant object</returns>
    internal static Plant SpawnPlant(SeedType seedType, SeedType imitaterType, int gridX, int gridY, bool spawnOnNetwork)
    {
        return SeedPacketSyncPatch.SpawnPlant(seedType, imitaterType, gridX, gridY, spawnOnNetwork);
    }

    /// <summary>
    /// Spawns a zombie at the specified grid position with optional network synchronization
    /// </summary>
    /// <param name="zombieType">Type of zombie to spawn</param>
    /// <param name="gridX">X grid coordinate (0-8)</param>
    /// <param name="gridY">Y grid coordinate (0-4)</param>
    /// <param name="spawnOnNetwork">Whether to create a network controller for multiplayer sync</param>
    /// <param name="shakeBush">If the bush on the row the zombie spawns in shakes</param>
    /// <returns>The spawned Zombie object</returns>
    internal static Zombie SpawnZombie(ZombieType zombieType, int gridX, int gridY, bool shakeBush, bool spawnOnNetwork)
    {
        return SeedPacketSyncPatch.SpawnZombie(zombieType, gridX, gridY, shakeBush, spawnOnNetwork);
    }

    /// <summary>
    /// Gets the opposite team for a given player team.
    /// </summary>
    /// <param name="team">The player team to get the opposite of.</param>
    /// <returns>
    /// The opposite team:
    /// <list type="bullet">
    /// <item><description>Plants → Zombies</description></item>
    /// <item><description>Zombies → Plants</description></item>
    /// <item><description>Any other value → None</description></item>
    /// </list>
    /// </returns>
    internal static PlayerTeam GetOppositeTeam(PlayerTeam team)
    {
        switch (team)
        {
            case PlayerTeam.Plants:
                return PlayerTeam.Zombies;
            case PlayerTeam.Zombies:
                return PlayerTeam.Plants;
            default:
                return PlayerTeam.None;
        }
    }
}

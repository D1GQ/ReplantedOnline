using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.TreeStateActivities;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object;
using ReplantedOnline.Network.Object.Game;
using static Il2CppReloaded.Constants;

namespace ReplantedOnline.Patches.Versus.NetworkSync;

[HarmonyPatch]
internal static class SeedPacketSyncPatch
{
    // Rework planting seeds to support RPCs
    // This actually took hours to find out what's doing what :(
    [HarmonyPatch(typeof(GameplayActivity), nameof(GameplayActivity.OnMouseDownBG))]
    [HarmonyPrefix]
    internal static bool Selected_Prefix(GameplayActivity __instance, int mouseButton, int playerIndex)
    {
        // Get the type of seed being planted
        var seedType = __instance.Board.GetSeedTypeInCursor(0);

        // Check if the player is currently holding a plant in their cursor
        if (seedType != SeedType.None)
        {
            // Get the mouse position and convert it to grid coordinates
            var pos = Instances.GameplayActivity.GetMousePosition();
            var gridX = Instances.GameplayActivity.Board.PixelToGridXKeepOnBoard(pos.x, pos.y);
            var gridY = Instances.GameplayActivity.Board.PixelToGridYKeepOnBoard(pos.x, pos.y);

            // Check if planting at this position is valid
            if (CanPlace(seedType, gridX, gridY))
            {
                // Find the seed packet from the seed bank that matches the seed type
                var packet = __instance.Board.mSeedBank.SeedPackets.FirstOrDefault(packet => packet.mPacketType == seedType);

                // Get the cost of the seed and check if player has enough sun
                var cost = packet.GetCost();
                if (__instance.Board.CanTakeSunMoney(cost, 0))
                {
                    // Mark the packet as used and deduct the sun cost
                    packet.WasPlanted(0);
                    __instance.Board.TakeSunMoney(cost, 0);
                    __instance.Board.ClearCursor();
                    PlaceSeed(seedType, packet.mImitaterType, gridX, gridY, true);
                    Instances.GameplayActivity.m_audioService.PlaySample(Sound.SOUND_PLANT);
                }

                // Return false to skip the original method since we've handled planting
                return false;
            }

            // If planting is not valid, play buzzer sound
            Instances.GameplayActivity.m_audioService.PlaySample(Sound.SOUND_BUZZER);

            // Return false to skip original method (invalid placement)
            return false;
        }

        // Return true to execute original method (no plant in cursor, normal behavior)
        return true;
    }

    private static bool CanPlace(SeedType seedType, int gridX, int gridY)
    {
        // Check if placing a Dancer zombie - they cannot be placed in top or bottom rows (0 and 4)
        var checkDancerGrid = seedType != SeedType.ZombieDancer || (gridY != 0 && gridY != 4);

        return Instances.GameplayActivity.Board.CanPlantAt(gridX, gridY, seedType) == PlantingReason.Ok
            && checkDancerGrid;
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
        // Check if this is a zombie seed (from I, Zombie mode)
        // Zombie seeds have special handling since they spawn zombies instead of plants
        if (Challenge.IsZombieSeedType(seedType))
        {
            // Convert seed type to actual zombie type
            // Example: SeedType.SEED_ZOMBIE_NORMAL -> ZombieType.ZOMBIE_NORMAL
            var type = Challenge.IZombieSeedTypeToZombieType(seedType);

            // Delegate to zombie spawning logic
            return SpawnZombie(type, gridX, gridY, spawnOnNetwork);
        }
        else
        {
            // This is a regular plant seed - delegate to plant spawning logic
            return SpawnPlant(seedType, imitaterType, gridX, gridY, spawnOnNetwork);
        }
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
        // Create the actual plant object in the game world using the original game method
        var plant = Instances.GameplayActivity.Board.AddPlant(gridX, gridY, seedType, imitaterType);

        // Only create network controller if network synchronization is requested
        // This prevents creating network objects in single-player mode
        if (spawnOnNetwork)
        {
            // Spawn a networked controller that will sync this plant across all clients
            var netClass = NetworkClass.SpawnNew<PlantNetworked>(net =>
            {
                net._Plant = plant;
                net.SeedType = seedType;
                net.ImitaterType = imitaterType;
                net.GridX = gridX;
                net.GridY = gridY;
            });

            PlantNetworked.NetworkedPlants[plant] = netClass;
        }

        return plant;
    }

    /// <summary>
    /// Spawns a zombie at the specified grid position with optional network synchronization
    /// </summary>
    /// <param name="zombieType">Type of zombie to spawn</param>
    /// <param name="gridX">X grid coordinate (0-8)</param>
    /// <param name="gridY">Y grid coordinate (0-4)</param>
    /// <param name="spawnOnNetwork">Whether to create a network controller for multiplayer sync</param>
    /// <returns>The spawned Zombie object</returns>
    internal static Il2CppReloaded.Gameplay.Zombie SpawnZombie(ZombieType zombieType, int gridX, int gridY, bool spawnOnNetwork)
    {
        // Determine if this zombie type rises from the ground (like grave zombies)
        // Bungee zombies are excluded from rising behavior even if they normally would
        var rise = VersusMode.ZombieRisesFromGround(zombieType) && zombieType != ZombieType.Bungee && zombieType != ZombieType.Target;

        // Some zombies have forced spawn positions on the right side
        var forceXPos = !VersusMode.ZombieRisesFromGround(zombieType);

        // Add zombie to the board at the specified position
        // Use forced X position (9) for certain zombies, otherwise use the provided gridX
        var zombie = Instances.GameplayActivity.Board.AddZombieAtCell(zombieType, forceXPos ? 9 : gridX, gridY);

        // If this zombie rises from ground, trigger the rising animation
        // This makes the zombie emerge from the ground rather than just appearing
        if (rise)
        {
            zombie.RiseFromGrave(gridX, gridY);
        }

        // Special handling for Backup Dancer zombies to set their exact X position
        if (zombieType == ZombieType.BackupDancer)
        {
            zombie.mPosX = gridX;
        }

        if (zombieType == ZombieType.Gravestone)
        {
            Instances.GameplayActivity.Board.m_vsGravestones.Add(zombie);
            zombie.mGraveX = gridX;
            zombie.mGraveY = gridY;
        }

        // Only create network controller if network synchronization is requested
        if (spawnOnNetwork)
        {
            // Spawn a networked controller that will sync this zombie across all clients
            var netClass = NetworkClass.SpawnNew<ZombieNetworked>(net =>
            {
                net._Zombie = zombie;
                net.ZombieID = zombie.DataID;
                net.ZombieType = zombieType;
                net.GridX = gridX;
                net.GridY = gridY;
            });

            ZombieNetworked.NetworkedZombies[zombie] = netClass;
        }

        return zombie;
    }
}
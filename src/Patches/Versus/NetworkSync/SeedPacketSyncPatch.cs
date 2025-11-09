using HarmonyLib;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.TreeStateActivities;
using Il2CppSource.Controllers;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Online;
using static Il2CppReloaded.Constants;

namespace ReplantedOnline.Patches.Versus.NetworkSync;

[HarmonyPatch]
internal static class SeedPacketSyncPatch
{
    [HarmonyPatch(typeof(GamepadCursorController), nameof(GamepadCursorController._onCursorConfirmed))]
    [HarmonyPrefix]
    private static bool _onCursorConfirmed_Prefix(GamepadCursorController __instance)
    {
        if (NetLobby.AmInLobby())
        {
            // Get the type of seed being planted
            var seedType = __instance.Board.GetSeedTypeInCursor(ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX);

            // Check if the player is currently holding a plant in their cursor
            if (seedType != SeedType.None)
            {
                // Disable Gamepad usage until the no cooldown bug can be fixed
                Instances.GameplayActivity.m_audioService.PlaySample(Sound.SOUND_BUZZER);
                return false;

                // Get the cursor position and convert it to grid coordinates
                var gridX = __instance.m_cursor.m_gridX;
                var gridY = __instance.m_cursor.m_gridY;

                // Check if planting at this position is valid
                if (CanPlace(seedType, gridX, gridY))
                {
                    // Find the seed packet from the seed bank that matches the seed type
                    var seedPacket = __instance.GetFirstSelectedSeedPack();

                    // Get the cost of the seed and check if player has enough sun
                    var cost = seedPacket.GetCost();
                    if (__instance.Board.CanTakeSunMoney(cost, ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX))
                    {
                        // Mark the packet as used and deduct the sun cost
                        seedPacket.WasPlanted(ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX); // TODO: Fix no cooldown on gamepad
                        __instance.Board.TakeSunMoney(cost, ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX);
                        PlaceSeed(seedType, seedPacket.mImitaterType, gridX, gridY, true);
                        Instances.GameplayActivity.m_audioService.PlaySample(Sound.SOUND_PLANT);
                    }
                    else
                    {
                        Instances.GameplayActivity.m_audioService.PlaySample(Sound.SOUND_BUZZER);
                    }

                    // Return false to skip the original method since we've handled planting
                    return false;
                }

                Instances.GameplayActivity.m_audioService.PlaySample(Sound.SOUND_BUZZER);

                // Return false to skip original method (invalid placement)
                return false;
            }
        }

        // Return true to execute original method (no plant in cursor, normal behavior)
        return true;
    }

    // Rework planting seeds to support RPCs
    // This actually took hours to find out what's doing what :(
    [HarmonyPatch(typeof(GameplayActivity), nameof(GameplayActivity.OnMouseDownBG))]
    [HarmonyPrefix]
    private static bool OnMouseDownBG_Prefix(GameplayActivity __instance, int mouseButton, int playerIndex)
    {
        if (NetLobby.AmInLobby())
        {
            // Get the type of seed being planted
            var seedType = __instance.Board.GetSeedTypeInCursor(ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX);

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
                    var seedPacket = __instance.Board.SeedBanks.LocalItem().SeedPackets.FirstOrDefault(packet => packet.mPacketType == seedType);

                    // Get the cost of the seed and check if player has enough sun
                    var cost = seedPacket.GetCost();
                    if (__instance.Board.CanTakeSunMoney(cost, ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX))
                    {
                        // Mark the packet as used and deduct the sun cost
                        seedPacket.WasPlanted(ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX);
                        __instance.Board.TakeSunMoney(cost, ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX);
                        __instance.Board.ClearCursor();
                        PlaceSeed(seedType, seedPacket.mImitaterType, gridX, gridY, true);
                        Instances.GameplayActivity.m_audioService.PlaySample(Sound.SOUND_PLANT);
                    }
                    else
                    {
                        Instances.GameplayActivity.m_audioService.PlaySample(Sound.SOUND_BUZZER);
                    }

                    // Return false to skip the original method since we've handled planting
                    return false;
                }

                // If planting is not valid, play buzzer sound
                Instances.GameplayActivity.m_audioService.PlaySample(Sound.SOUND_BUZZER);

                // Return false to skip original method (invalid placement)
                return false;
            }
        }

        // Return true to execute original method (no plant in cursor, normal behavior)
        return true;
    }

    private static bool CanPlace(SeedType seedType, int gridX, int gridY)
    {
        // Check if placing a Dancer zombie - they cannot be placed in top or bottom rows (0 and 4)
        var checkDancerGrid = seedType != SeedType.ZombieDancer || (gridY != 0 && gridY != 4);

        return Instances.GameplayActivity.Board.CanPlantAt(gridX, gridY, seedType) == PlantingReason.Ok
            && VersusState.VersusPhase is (VersusPhase.Gameplay or VersusPhase.SuddenDeath)
            && checkDancerGrid;
    }

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
            return SpawnZombie(type, gridX, gridY, false, spawnOnNetwork);
        }
        else
        {
            // This is a regular plant seed - delegate to plant spawning logic
            return SpawnPlant(seedType, imitaterType, gridX, gridY, spawnOnNetwork);
        }
    }

    internal static Plant SpawnPlant(SeedType seedType, SeedType imitaterType, int gridX, int gridY, bool spawnOnNetwork)
    {
        // Create the actual plant object in the game world using the original game method
        var plant = Instances.GameplayActivity.Board.AddPlant(gridX, gridY, seedType, imitaterType);

        Instances.GameplayActivity.Board.m_plants.NewArrayItem(plant, plant.DataID);

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
            }, VersusState.PlantSteamId);
            plant.AddNetworkedLookup(netClass);
            netClass.name = $"{Enum.GetName(plant.mSeedType)}_Plant ({netClass.NetworkId})";
        }

        return plant;
    }

    internal static Il2CppReloaded.Gameplay.Zombie SpawnZombie(ZombieType zombieType, int gridX, int gridY, bool shakeBush, bool spawnOnNetwork)
    {
        // Determine if this zombie type rises from the ground (like grave zombies)
        // Bungee zombies are excluded from rising behavior even if they normally would
        var rise = VersusMode.ZombieRisesFromGround(zombieType) && zombieType != ZombieType.Bungee && zombieType != ZombieType.Target;

        // Some zombies have forced spawn positions on the right side
        var forceXPos = !VersusMode.ZombieRisesFromGround(zombieType);

        // Add zombie to the board at the specified position
        // Use forced X position (9) for certain zombies, otherwise use the provided gridX
        var zombie = Instances.GameplayActivity.Board.AddZombieAtCell(zombieType, forceXPos ? 9 : gridX, gridY);

        Instances.GameplayActivity.Board.m_zombies.NewArrayItem(zombie, zombie.DataID);

        // If this zombie rises from ground, trigger the rising animation
        // This makes the zombie emerge from the ground rather than just appearing
        if (rise && !shakeBush)
        {
            zombie.RiseFromGrave(gridX, gridY);
        }

        if (shakeBush)
        {
            Instances.GameplayActivity.BackgroundController.ZombieSpawnedInRow(gridY);
        }

        // Special handling for Backup Dancer zombies to set their exact X position
        if (zombieType == ZombieType.BackupDancer)
        {
            zombie.mPosX = gridX;
        }

        // Set Gravestone grid pos
        if (zombieType == ZombieType.Gravestone)
        {
            Instances.GameplayActivity.Board.m_vsGravestones.Add(zombie);
            zombie.mGraveX = gridX;
            zombie.mGraveY = gridY;
        }

        // Set Bungee grid target
        if (zombieType == ZombieType.Bungee)
        {
            zombie.mTargetCol = gridX;
            zombie.mTargetRow = gridY;
        }

        // Only create network controller if network synchronization is requested
        if (spawnOnNetwork)
        {
            // Spawn a networked controller that will sync this zombie across all clients
            var netClass = NetworkClass.SpawnNew<ZombieNetworked>(net =>
            {
                net._Zombie = zombie;
                net.ZombieType = zombieType;
                net.ZombieSpeed = zombie.mVelX;
                net.ShakeBush = shakeBush;
                net.GridX = gridX;
                net.GridY = gridY;
            }, VersusState.PlantSteamId);
            zombie.AddNetworkedLookup(netClass);
            netClass.name = $"{Enum.GetName(zombie.mZombieType)}_Zombie ({netClass.NetworkId})";
        }

        // Fix rendering issues
        if (zombieType is ZombieType.Gravestone)
        {
            zombie.RenderOrder -= 100 + (5 * (gridY + 1));
        }
        else if (zombieType is ZombieType.Target)
        {
            zombie.RenderOrder -= 200 + (10 * (gridY + 1));
        }

        return zombie;
    }
}
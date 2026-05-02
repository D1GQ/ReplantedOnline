using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities;
using UnityEngine;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class FlagZombiePatch
{
    private readonly static ExecuteInterval spawnInterval = new();
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.ZombieInitialize))]
    [HarmonyPrefix]
    private static void Zombie_ZombieInitialize_Prefix(ZombieType theType)
    {
        if (theType != ZombieType.Flag) return;

        if (ReplantedLobby.AmInLobby())
        {
            if (ReplantedLobby.AmLobbyHost())
            {
                if (spawnInterval.Execute())
                {
                    SpawnZombieWave();
                }
            }

            Instances.GameplayActivity.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_HUGE_WAVE);
        }
    }

    /// <summary>
    /// Spawns a wave of zombies.
    /// </summary>
    private static void SpawnZombieWave()
    {
        // Determine spawn count and board dimensions
        int amount = VersusGameplayManager.FlagSpawnAmount();
        int rows = Instances.GameplayActivity.Board.GetNumRows();

        // Get weighted special zombie spawn rules
        List<FlagZombieSpecialSpawn> specialSpawns = VersusGameplayManager.GetFlagZombieSpawns();

        List<int> order = [.. Enumerable.Range(0, rows)];

        for (int i = 0; i < amount; i++)
        {
            // Shuffle once per full pass to distribute zombies evenly across rows
            ShuffleIfNeeded(ref order, rows, i);

            // Select the next row from the shuffled cycle
            int y = GetRow(order, rows, i);

            // After first full cycle, occasionally shift to adjacent rows for variation
            y = AdjustRowWithNeighborShift(y, rows, i);

            // Determine final zombie type
            ZombieType type = PickZombieType(specialSpawns);

            // Spawn and network the zombie
            SpawnZombie(type, y, specialSpawns);
        }
    }

    /// <summary>
    /// Performs a Fisher–Yates shuffle at the start of each cycle.
    /// This guarantees even distribution while still introducing randomness.
    /// </summary>
    private static void ShuffleIfNeeded(ref List<int> order, int rows, int index)
    {
        if (index % rows != 0)
            return;

        order = [.. order.Shuffle()];
    }

    /// <summary>
    /// Retrieves the next row in the current shuffled cycle.
    /// Cycles back to the start once all rows have been used.
    /// </summary>
    private static int GetRow(List<int> order, int rows, int index)
    {
        return order[index % rows];
    }

    /// <summary>
    /// Introduces slight randomness after the first cycle by occasionally
    /// shifting the spawn to a neighboring row. This keeps early distribution fair
    /// while preventing later waves from feeling too predictable.
    /// </summary>
    private static int AdjustRowWithNeighborShift(int y, int rows, int index)
    {
        if (index < rows)
            return y;

        if (Common.RandRangeInt(0, 100) >= 15)
            return y;

        int direction = Common.RandRangeInt(0, 1) == 0 ? -1 : 1;

        return Mathf.Clamp(y + direction, 0, rows - 1);
    }

    /// <summary>
    /// Selects a zombie type based on special spawn rules.
    /// Falls back to Normal if no special condition is met.
    /// </summary>
    private static ZombieType PickZombieType(List<FlagZombieSpecialSpawn> specialSpawns, bool excludeBungee = false)
    {
        foreach (var specialSpawn in specialSpawns)
        {
            if (excludeBungee)
            {
                if (specialSpawn.ZombieType == ZombieType.Bungee)
                {
                    continue;
                }
            }

            if (specialSpawn.Pick())
                return specialSpawn.ZombieType;
        }

        return ZombieType.Normal;
    }

    /// <summary>
    /// Handles local spawn setup and synchronizes the zombie across the network.
    /// Applies a small random X offset for natural positioning.
    /// </summary>
    private static void SpawnZombie(ZombieType type, int y, List<FlagZombieSpecialSpawn> specialSpawns)
    {
        if (type != ZombieType.Bungee)
        {
            var zombie = SeedPacketDefinitions.SpawnZombie(type, 9, y, SpawnType.BackgroundAndShakeBushes, false).Zombie;
            zombie.mPosX += Common.RandRangeInt(20, 70);

            var zombieNetworked = SeedPacketDefinitions.SpawnZombieOnNetwork(zombie, 9, y, SpawnType.BackgroundAndShakeBushes);
            zombieNetworked.SendSnapToPosRpc();
        }
        else
        {
            // Pick a target column for the Bungee Zombie
            bool spawningZombie = Common.RandRangeInt(0, 100) > 15;
            int row = PickBungeeColumn(Instances.GameplayActivity.Board, y, spawningZombie);
            if (row == -1)
            {
                // fallback to other zombie tyoe if no valid bungee targets
                var zombieToSpawn = PickZombieType(specialSpawns, true);
                SpawnZombie(zombieToSpawn, y, specialSpawns);
                return;
            }

            if (!spawningZombie)
            {
                // Spawn Bungee to take plant
                SeedPacketDefinitions.SpawnZombie(type, row, y, SpawnType.None, true);
            }
            else
            {
                // Drop random zombie from wave
                var zombieToDrop = PickZombieType(specialSpawns, true);
                SeedPacketDefinitions.SpawnZombie(zombieToDrop, row, y, SpawnType.BungeeDropZombie, true);
            }
        }
    }

    /// <summary>
    /// Selects a target column for a Bungee Zombie using weighted random selection based on plant values.
    /// Recreates the original game logic from PickBungeeZombieTarget.
    /// </summary>
    private static int PickBungeeColumn(Board board, int targetGridY, bool spawningZombie)
    {
        int sunflowerCount = board.CountSunFlowers();

        List<(int gridX, int weight)> targets = [];

        // Check columns in this row
        if (!spawningZombie)
        {
            for (int gridX = 0; gridX < 6; gridX++)
            {
                if (board.BungeeIsTargetingCell(gridX, targetGridY)) continue;
                int colWeight = 0;

                Plant plant = board.GetTopPlantAt(gridX, targetGridY, PlantPriority.BungeeOrder);

                if (plant != null)
                {
                    if (plant.mSeedType is SeedType.Flowerpot or SeedType.Lilypad or SeedType.GiantWallnut)
                        continue;

                    if (SeedPacketDefinitions.CurrencyProducingSeedTypes.Contains(plant.mSeedType))
                    {
                        colWeight += (sunflowerCount == 1) ? 1 : 10000;
                    }
                    else
                    {
                        colWeight += 10000;
                    }

                    if (colWeight > 0)
                    {
                        targets.Add((gridX, colWeight));
                    }
                }
            }
        }
        else
        {
            for (int gridX = 5; gridX > 2; gridX--)
            {
                if (board.BungeeIsTargetingCell(gridX, targetGridY)) continue;

                Plant plant = board.GetTopPlantAt(gridX, targetGridY, PlantPriority.BungeeOrder);

                if (plant != null)
                {
                    if (plant.mSeedType is SeedType.Flowerpot or SeedType.Lilypad)
                        continue;

                    int weight = 10000 - (gridX * 1500);
                    if (plant.mSeedType is SeedType.Wallnut or SeedType.GiantWallnut)
                    {
                        // Spawn zombie over wallnuts
                        targets.Add((gridX - 1, weight));
                    }
                    else
                    {
                        // Spawn zombie infront of other plants
                        targets.Add((gridX + 1, weight));
                    }
                    continue;
                }
            }

            // If no plants to target, spawn close
            if (targets.Count == 0)
            {
                return 2;
            }
        }

        // No valid targets
        if (targets.Count == 0)
            return -1;

        // Weighted random pick
        int totalWeight = 0;
        foreach (var (gridX, weight) in targets)
            totalWeight += weight;

        int randomValue = Common.Rand(totalWeight);
        int cumulative = 0;

        for (int i = 0; i < targets.Count; i++)
        {
            var (gridX, weight) = targets[i];
            cumulative += weight;
            if (randomValue < cumulative)
                return gridX;
        }

        return targets[0].gridX;
    }
}
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
            SpawnZombie(type, y);
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
    private static ZombieType PickZombieType(List<FlagZombieSpecialSpawn> specialSpawns)
    {
        foreach (var specialSpawn in specialSpawns)
        {
            if (specialSpawn.Pick())
                return specialSpawn.ZombieType;
        }

        return ZombieType.Normal;
    }

    /// <summary>
    /// Handles local spawn setup and synchronizes the zombie across the network.
    /// Applies a small random X offset for natural positioning.
    /// </summary>
    private static void SpawnZombie(ZombieType type, int y)
    {
        var zombie = SeedPacketDefinitions.SpawnZombie(type, 9, y, SpawnType.BackgroundAndShakeBushes, false).Zombie;
        zombie.mPosX += Common.RandRangeInt(20, 70);

        var zombieNetworked = SeedPacketDefinitions.SpawnZombieOnNetwork(zombie, 9, y, SpawnType.BackgroundAndShakeBushes);
        zombieNetworked.SendSnapToPosRpc();
    }
}
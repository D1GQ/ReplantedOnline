using HarmonyLib;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Modules.Versus;
using ReplantedOnline.Network.Client;
using UnityEngine;

namespace ReplantedOnline.Patches.Gameplay.Versus.Zombies;

[HarmonyPatch]
internal static class FlagZombiePatch
{
    private static uint spawnInterval;
    [HarmonyPatch(typeof(Zombie), nameof(Zombie.ZombieInitialize))]
    [HarmonyPrefix]
    private static void Zombie_ZombieInitialize_Prefix(ZombieType theType)
    {
        if (theType != ZombieType.Flag) return;

        if (ReplantedLobby.AmInLobby())
        {
            if (ReplantedLobby.AmLobbyHost())
            {
                spawnInterval++;
                if (spawnInterval % 2 != 0)
                {
                    SpawnZombies();
                }
            }

            Instances.GameplayActivity.PlaySample(Il2CppReloaded.Constants.Sound.SOUND_HUGE_WAVE);
        }
    }

    private static void SpawnZombies()
    {
        // Get how many zombies to spawn and how many rows exist
        int amount = VersusGameplayManager.FlagSpawnAmount();
        int rows = Instances.GameplayActivity.Board.GetNumRows();

        // Create initial ordered row list
        List<int> order = [.. Enumerable.Range(0, rows)];

        // Get special zombie spawn chances
        List<FlagZombieSpecialSpawn> specialSpawns = VersusGameplayManager.GetFlagZombieSpawns();

        for (int i = 0; i < amount; i++)
        {
            // Every full cycle, reshuffle rows
            if (i % rows == 0)
            {
                for (int s = order.Count - 1; s > 0; s--)
                {
                    int j = Common.RandRangeInt(0, s);
                    (order[s], order[j]) = (order[j], order[s]);
                }
            }

            // Pick row in cyclic shuffled order
            int y = order[i % rows];

            // Only allow neighbor shifting After first cycle, ensures at least one zombie per row
            if (i >= rows && Common.RandRangeInt(0, 100) < 15)
            {
                y = Mathf.Clamp(
                    y + (Common.RandRangeInt(0, 1) == 0 ? -1 : 1),
                    0, rows - 1
                );
            }

            // Default zombie type
            ZombieType type = ZombieType.Normal;

            // Try to replace with a special zombie
            foreach (var specialSpawn in specialSpawns)
            {
                if (specialSpawn.Pick())
                {
                    type = specialSpawn.ZombieType;
                    break;
                }
            }

            // Spawn zombie
            var z = SeedPacketDefinitions.SpawnZombie(type, 9, y, true, false);
            z.mPosX += Common.RandRangeInt(20, 70);

            var zombieNetworked = SeedPacketDefinitions.SpawnZombieOnNetwork(z, 9, y, true);
            zombieNetworked.SendSnapToPosRpc();
        }
    }

    internal sealed class FlagZombieSpecialSpawn(ZombieType zombieType, int chance = 100, int decreaseBy = 0)
    {
        internal readonly ZombieType ZombieType = zombieType;

        private int _chance = chance;
        private readonly int _decreaseBy = decreaseBy;

        internal bool Pick()
        {
            if (_chance <= 0) return false;

            if (Common.RandRangeInt(1, 100) <= _chance)
            {
                _chance = Math.Max(_chance - _decreaseBy, 0);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
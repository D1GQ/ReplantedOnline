using Il2CppReloaded.Gameplay;
using Il2CppSource.Utils;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Patches.Gameplay.UI;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Modules.Versus.Gamemodes;

/// <summary>
/// Versus Gamemode that has random seed and zombie packets.
/// </summary>
[RegisterVersusGameMode]
internal sealed class RandomGamemode : IVersusGamemode
{
    /// <inheritdoc/>
    public void OnGameModeStart(VersusMode versusMode)
    {
        VersusLobbyPatch.VsSideChooser?.gameObject?.SetActive(false);
        versusMode.Phase = VersusPhase.Gameplay;
        StateTransitionUtils.Transition("InGame");
    }

    /// <inheritdoc/>
    public void OnGameplayStart(VersusMode versusMode)
    {
        IArenaSetupSeedbank.AddInitialSeedsToBanks();

        if (VersusState.AmPlantSide)
        {
            var plantSeeds = Enum.GetValues<SeedType>().Where(seed =>
                !Challenge.IsZombieSeedType(seed) &&
                !IArenaSetupSeedbank.ExcludeSeedFromRandom(seed) &&
                !SeedPacketDefinitions.NoneSeedTypes.Contains(seed) &&
                !SeedPacketDefinitions.ExcludeFromRandomSeedTypes.Contains(seed) &&
                Instances.DataServiceActivity.Service.GetPlantDefinition(seed).VersusCost > 0
            );

            int numSeedsToAdd = versusMode.m_board.SeedBanks.LocalItem().NumPackets - versusMode.m_board.SeedBanks.LocalItem().GetPacketCount();
            var shuffledSeeds = plantSeeds.Shuffle().ToList();

            if (VersusState.Arena is not (ArenaTypes.Night or ArenaTypes.RoofNight))
            {
                var potentialSeeds = shuffledSeeds.Take(numSeedsToAdd).ToList();
                bool hasInstantCoffee = potentialSeeds.Contains(SeedType.InstantCoffee);
                bool hasSleepingPlant = potentialSeeds.Any(seed => SeedPacketDefinitions.SleepingPlants.Contains(seed));

                if (!hasInstantCoffee)
                {
                    // Sleeping plants without Instant Coffee
                    shuffledSeeds.RemoveAll(seed => SeedPacketDefinitions.SleepingPlants.Contains(seed));
                }

                if (!hasSleepingPlant)
                {
                    // Instant Coffee without sleeping plants
                    shuffledSeeds.Remove(SeedType.InstantCoffee);
                }

                shuffledSeeds = [.. shuffledSeeds.Take(numSeedsToAdd)];
            }
            else
            {
                shuffledSeeds.Remove(SeedType.InstantCoffee);
                shuffledSeeds = [.. shuffledSeeds.Take(numSeedsToAdd)];
            }

            foreach (var seedType in shuffledSeeds)
            {
                versusMode.m_board.SeedBanks.LocalItem().AddSeed(seedType, true);
            }
        }
        else if (VersusState.AmZombieSide)
        {
            var zombieSeeds = Enum.GetValues<SeedType>().Where(seed =>
                Challenge.IsZombieSeedType(seed) &&
                !IArenaSetupSeedbank.ExcludeSeedFromRandom(seed) &&
                !SeedPacketDefinitions.NoneSeedTypes.Contains(seed) &&
                !SeedPacketDefinitions.ExcludeFromRandomSeedTypes.Contains(seed) &&
                Instances.DataServiceActivity.Service.GetPlantDefinition(seed).VersusCost > 0
            );

            int numSeedsToAdd = versusMode.m_board.SeedBanks.LocalItem().NumPackets - versusMode.m_board.SeedBanks.LocalItem().GetPacketCount();
            var shuffledSeeds = zombieSeeds.Shuffle().Take(numSeedsToAdd).ToArray();
            foreach (var seedType in shuffledSeeds)
            {
                versusMode.m_board.SeedBanks.LocalItem().AddSeed(seedType, true);
            }
        }

        // Set opponent seeds to hide, which will be revealed once SyncSeedPacketRpc.cs is received
        for (int i = IArenaSetupSeedbank.GetStartingSeedPacketCount(); i < Instances.GameplayActivity.Board.SeedBanks.OpponentItem().SeedPackets.Count; i++)
        {
            SeedPacket seedPacket = Instances.GameplayActivity.Board.SeedBanks.OpponentItem().SeedPackets[i];
            seedPacket.mActive = false;
            seedPacket.PacketType = SeedPacketDefinitions.RandomHiddenSeed;
        }
    }

    /// <inheritdoc/>
    public void UpdateGameplay(VersusMode versusMode) { }

    /// <inheritdoc/>
    public void OnGameplayEnd(VersusMode versusMode, PlayerTeam winningTeam) { }
}

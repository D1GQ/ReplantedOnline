using Il2CppReloaded.Gameplay;
using Il2CppSource.Utils;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
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
        if (VersusState.AmPlantSide)
        {
            var plantSeeds = Enum.GetValues<SeedType>().Where(seed =>
                !Challenge.IsZombieSeedType(seed) &&
                !SeedPacketDefinitions.ExcludeFromRandom.Contains(seed) &&
                !SeedPacketDefinitions.DisabledSeedTypes.Contains(seed) &&
                Instances.DataServiceActivity.Service.GetPlantDefinition(seed).VersusCost > 0
            );

            var shuffledSeeds = plantSeeds.OrderBy(x => Guid.NewGuid()).ToList();

            versusMode.m_board.SeedBanks.LocalItem().AddSeed(SeedType.Sunflower, true);

            for (int i = 0; i < 5 && i < shuffledSeeds.Count; i++)
            {
                var seedType = shuffledSeeds[i];
                versusMode.m_board.SeedBanks.LocalItem().AddSeed(seedType, true);
            }
        }
        else if (VersusState.AmZombieSide)
        {
            var zombieSeeds = Enum.GetValues<SeedType>().Where(seed =>
                Challenge.IsZombieSeedType(seed) &&
                !SeedPacketDefinitions.ExcludeFromRandom.Contains(seed) &&
                !SeedPacketDefinitions.DisabledSeedTypes.Contains(seed) &&
                Instances.DataServiceActivity.Service.GetPlantDefinition(seed).VersusCost > 0
            );

            var shuffledSeeds = zombieSeeds.OrderBy(x => Guid.NewGuid()).ToList();

            versusMode.m_board.SeedBanks.LocalItem().AddSeed(SeedType.ZombieGravestone, true);

            for (int i = 0; i < 5 && i < shuffledSeeds.Count; i++)
            {
                var seedType = shuffledSeeds[i];
                versusMode.m_board.SeedBanks.LocalItem().AddSeed(seedType, true);
            }
        }
    }

    /// <inheritdoc/>
    public void UpdateGameplay(VersusMode versusMode) { }

    /// <inheritdoc/>
    public void OnGameplayEnd(VersusMode versusMode, PlayerTeam winningTeam) { }
}

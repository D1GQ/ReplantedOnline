using Il2CppReloaded.Gameplay;
using Il2CppSource.Utils;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Interfaces;
using ReplantedOnline.Patches.Gameplay.UI;
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Modules.Versus.Gamemodes;

/// <summary>
/// Versus Gamemode that has preset of seed and zombie packets.
/// </summary>
[RegisterVersusGameMode]
internal sealed class QuickplayGamemode : IVersusGamemode
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
            foreach (var seedType in versusMode.m_quickPlayPlants)
            {
                versusMode.m_board.SeedBanks.LocalItem().AddSeed(seedType, true);
            }

            foreach (var seedType in versusMode.m_quickPlayZombies)
            {
                versusMode.m_board.SeedBanks.OpponentItem().AddSeed(seedType, true);
            }
        }
        else if (VersusState.AmZombieSide)
        {
            foreach (var seedType in versusMode.m_quickPlayZombies)
            {
                versusMode.m_board.SeedBanks.LocalItem().AddSeed(seedType, true);
            }

            foreach (var seedType in versusMode.m_quickPlayPlants)
            {
                versusMode.m_board.SeedBanks.OpponentItem().AddSeed(seedType, true);
            }
        }
    }

    /// <inheritdoc/>
    public void UpdateGameplay(VersusMode versusMode) { }

    /// <inheritdoc/>
    public void OnGameplayEnd(VersusMode versusMode, PlayerTeam winningTeam) { }
}

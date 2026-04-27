using Il2CppReloaded.Gameplay;
using Il2CppSource.Utils;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Network.Client;
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
        if (ReplantedClientData.LocalClient.Team == PlayerTeam.Plants)
        {
            foreach (var seedType in IArenaSetupSeedbank.GetQuickPlayPlants())
            {
                PvZRUtils.GetLocalSeedBankInfo().mSeedBank.AddSeed(seedType, true);
            }
            foreach (var seedType in IArenaSetupSeedbank.GetQuickPlayZombies())
            {
                PvZRUtils.GetOpponentSeedBankInfo().mSeedBank.AddSeed(seedType, true);
            }
        }
        else if (ReplantedClientData.LocalClient.Team == PlayerTeam.Zombies)
        {
            foreach (var seedType in IArenaSetupSeedbank.GetQuickPlayZombies())
            {
                PvZRUtils.GetLocalSeedBankInfo().mSeedBank.AddSeed(seedType, true);
            }
            foreach (var seedType in IArenaSetupSeedbank.GetQuickPlayPlants())
            {
                PvZRUtils.GetOpponentSeedBankInfo().mSeedBank.AddSeed(seedType, true);
            }
        }
    }

    /// <inheritdoc/>
    public void UpdateGameplay(VersusMode versusMode) { }

    /// <inheritdoc/>
    public void OnGameplayEnd(VersusMode versusMode, PlayerTeam winningTeam) { }
}

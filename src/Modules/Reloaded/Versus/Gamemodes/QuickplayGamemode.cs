using Il2CppReloaded.Gameplay;
using Il2CppSource.Utils;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Patches.Reloaded.Gameplay.UI;
using ReplantedOnline.Utilities.Modded;

namespace ReplantedOnline.Modules.Reloaded.Versus.Gamemodes;

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
        var plantSeedBankInfo = PvZRUtils.GetPlantSeedBankInfo();
        var zombieSeedBankInfo = PvZRUtils.GetZombieSeedBankInfo();
        foreach (var seedType in IArenaSetupSeedbank.GetQuickPlayPlants())
        {
            plantSeedBankInfo.mSeedBank.AddSeed(seedType, true);
        }
        foreach (var seedType in IArenaSetupSeedbank.GetQuickPlayZombies())
        {
            zombieSeedBankInfo.mSeedBank.AddSeed(seedType, true);
        }
    }

    /// <inheritdoc/>
    public void UpdateGameplay(VersusMode versusMode) { }

    /// <inheritdoc/>
    public void OnGameplayEnd(VersusMode versusMode, PlayerTeam winningTeam) { }
}

#pragma warning disable CS0162

using Il2Cpp;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Input;
using Il2CppSource.Binders;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities.Modded;
using ReplantedOnline.Utilities.Unity;
using System.Collections;

namespace ReplantedOnline.Modules.Reloaded.Versus.Gamemodes;

/// <summary>
/// Versus Gamemode that allows the players to choose their seed and zombie packets.
/// </summary>
[RegisterVersusGameMode]
internal sealed class CustomGamemode : IVersusGamemode
{
    /// <inheritdoc/>
    public void OnGameModeStart(VersusMode versusMode)
    {
        ReloadedClientData.LocalClient?.Ready.Value = false;
        versusMode.Phase = VersusPhase.ChooseZombiePacket;
        Transitions.ToChooseSeeds();
        Instances.GameplayActivity.StartCoroutine(CoWaitSeedChooserVSSwap());
    }

    /// <inheritdoc/>
    public void OnGameplayStart(VersusMode versusMode) { }

    /// <inheritdoc/>
    public void UpdateGameplay(VersusMode versusMode) { }

    /// <inheritdoc/>
    public void OnGameplayEnd(VersusMode versusMode, PlayerTeam winningTeam) { }

    // Make Zombie have first pick in Custom
    private static IEnumerator CoWaitSeedChooserVSSwap()
    {
        // Wait for SeedChooserVSSwap to exist or exit if not in lobby
        while (UnityEngine.Object.FindObjectOfType<SeedChooserVSSwap>() == null)
        {
            if (!ReloadedLobby.AmInLobby())
            {
                yield break;
            }
            yield return null;
        }

        List<ChosenSeed> chosenSeeds = [
            .. Instances.GameplayActivity.SeedChooserScreen.mChosenSeeds,
            .. Instances.GameplayActivity.SeedChooserScreen.mChosenZombies,
        ];

        IArenaSetupSeedbank.AddInitialSeedsToBanks();
        ReloadedClientData.LocalClient?.Ready.Value = true;

        if (ModInfo.DEBUG && ReloadedLobby.GetLobbyMemberCount() == 1)
        {
            if (VersusState.AmPlantSide)
            {
                SetupDebugPlantSide();
            }

            if (VersusState.AmZombieSide)
            {
                SetupDebugZombieSide();
                SetupTurnOrder();
            }
        }
        else
        {
            SetupTurnOrder();
        }

        HideDisallowedSeeds(chosenSeeds);
    }

    private static void SetupDebugPlantSide()
    {
        var opponentSeedBankInfo = PvZRUtils.GetOpponentSeedBankInfo();
        SeedType[] quickPlayZombies = IArenaSetupSeedbank.GetQuickPlayZombies();

        for (int i = opponentSeedBankInfo.mSeedsInBank; i < quickPlayZombies.Length; i++)
        {
            opponentSeedBankInfo.AddSeedFromChooser(quickPlayZombies[i]);
        }

        var seedChooserVSSwap = UnityEngine.Object.FindObjectOfType<SeedChooserVSSwap>();
        seedChooserVSSwap.playerTurn = 1;
        seedChooserVSSwap.GetComponent<VersusChooserSwapBinder>().PlayerTurn = 1;
        Instances.GameplayActivity.VersusMode.Phase = VersusPhase.ChoosePlantPacket;

        var versusChooserSwapBinder = seedChooserVSSwap.GetComponent<VersusChooserSwapBinder>();
        var layer = versusChooserSwapBinder.m_player1Input.Cast<IInputNavigationLayer>();
        InputNavigationManager.Instance.RemoveLayer(layer);
    }

    private static void SetupDebugZombieSide()
    {
        var opponentSeedBankInfo = PvZRUtils.GetOpponentSeedBankInfo();
        SeedType[] quickPlayPlants = IArenaSetupSeedbank.GetQuickPlayPlants();

        for (int i = opponentSeedBankInfo.mSeedsInBank; i < quickPlayPlants.Length; i++)
        {
            opponentSeedBankInfo.AddSeedFromChooser(quickPlayPlants[i]);
        }
    }

    private static void SetupTurnOrder()
    {
        var seedChooserVSSwap = UnityEngine.Object.FindObjectOfType<SeedChooserVSSwap>();
        seedChooserVSSwap.swapCanvasOrder();
        seedChooserVSSwap.m_vsSeedChooserAnimator.Play(-160334332, 0, 1f);
        seedChooserVSSwap.playerTurn = 1;

        var versusChooserSwapBinder = seedChooserVSSwap.GetComponent<VersusChooserSwapBinder>();
        versusChooserSwapBinder.PlayerTurn = 1;

        if (VersusState.AmZombieSide)
        {
            seedChooserVSSwap.transform.Find("SeedChooser_Player2/IgnoreInput")?.gameObject.SetActive(false);
            versusChooserSwapBinder.m_player2Input.IgnoreInput1 = false;
            InputNavigationManager.Instance.SetCurrentLayer(versusChooserSwapBinder.m_player2Input.Cast<IInputNavigationLayer>());
            versusChooserSwapBinder.m_player1Input.IgnoreInput1 = true;
        }
        else if (VersusState.AmPlantSide)
        {
            InputNavigationManager.Instance.RemoveLayer(versusChooserSwapBinder.m_player1Input.Cast<IInputNavigationLayer>());
            versusChooserSwapBinder.m_player2Input.IgnoreInput1 = true;
        }
    }

    private static void HideDisallowedSeeds(List<ChosenSeed> chosenSeeds)
    {
        foreach (var seedPacket in chosenSeeds)
        {
            if (!ICharacterConfig.IsAllowedInArena(seedPacket.mSeedType, VersusState.Arena))
            {
                seedPacket.mSeedState = ChosenSeedState.SeedPacketHidden;
            }
        }
    }
}

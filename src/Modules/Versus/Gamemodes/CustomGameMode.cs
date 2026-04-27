#pragma warning disable CS0162

using Il2Cpp;
using Il2CppReloaded.Gameplay;
using Il2CppSource.Binders;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities;
using System.Collections;

namespace ReplantedOnline.Modules.Versus.Gamemodes;

/// <summary>
/// Versus Gamemode that allows the players to choose their seed and zombie packets.
/// </summary>
[RegisterVersusGameMode]
internal sealed class CustomGamemode : IVersusGamemode
{
    /// <inheritdoc/>
    public void OnGameModeStart(VersusMode versusMode)
    {
        ReplantedClientData.LocalClient?.Ready.Value = false;
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
        while (UnityEngine.Object.FindObjectOfType<SeedChooserVSSwap>() == null)
        {
            if (!ReplantedLobby.AmInLobby())
            {
                yield break;
            }

            yield return null;
        }

        // Remove initial seeds from vanilla
        foreach (var seedBack in Instances.GameplayActivity.Board.SeedBanks.m_values)
        {
            seedBack.RemoveSeed(0);
        }
        foreach (var seedBankInfo in Instances.GameplayActivity.SeedChooserScreen.m_seedBankInfos)
        {
            seedBankInfo.mSeedsInBank = 0;
        }

        List<ChosenSeed> chosenSeeds = [
            .. Instances.GameplayActivity.SeedChooserScreen.mChosenSeeds,
            .. Instances.GameplayActivity.SeedChooserScreen.mChosenZombies,
        ];

        foreach (var seedPacket in chosenSeeds)
        {
            if (seedPacket.mSeedState == ChosenSeedState.SeedInBank)
            {
                seedPacket.mSeedState = ChosenSeedState.SeedInChooser;
            }
        }

        // Add custom initial seeds
        var localSeedBankInfo = PvZRUtils.GetLocalSeedBankInfo();
        var opponentSeedBankInfo = PvZRUtils.GetOpponentSeedBankInfo();
        localSeedBankInfo.mSeedsInBank = 0;
        opponentSeedBankInfo.mSeedsInBank = 0;
        if (IArena.GetCurrentArena() is ISetupSeedbank setupSeedbank)
        {
            setupSeedbank.SetupSeedbank(localSeedBankInfo, ReplantedClientData.LocalClient.Team);
            setupSeedbank.SetupSeedbank(opponentSeedBankInfo, ReplantedClientData.LocalClient.Team.GetOppositeTeam());
        }
        else
        {
            ISetupSeedbank.BaseSetupSeedbank(localSeedBankInfo, ReplantedClientData.LocalClient.Team);
            ISetupSeedbank.BaseSetupSeedbank(opponentSeedBankInfo, ReplantedClientData.LocalClient.Team.GetOppositeTeam());
        }

        ReplantedClientData.LocalClient?.Ready.Value = true;

        if (ModInfo.DEBUG)
        {
            if (ReplantedLobby.GetLobbyMemberCount() == 1)
            {
                // Set up opponent seed bank for debugging
                if (VersusState.AmPlantSide)
                {
                    foreach (var seedType in Instances.GameplayActivity.VersusMode.m_quickPlayZombies.Skip(1))
                    {
                        Instances.GameplayActivity.Board.SeedBanks.OpponentItem().AddSeed(seedType, true);
                    }

                    var seedChooserVSSwapDebug = UnityEngine.Object.FindObjectOfType<SeedChooserVSSwap>();
                    seedChooserVSSwapDebug.playerTurn = 1;
                    seedChooserVSSwapDebug.GetComponent<VersusChooserSwapBinder>().PlayerTurn = 1;
                    DisableSeedPackets();
                    Instances.GameplayActivity.VersusMode.Phase = VersusPhase.ChoosePlantPacket;

                    yield break;
                }
                else if (VersusState.AmZombieSide)
                {
                    foreach (var seedType in Instances.GameplayActivity.VersusMode.m_quickPlayPlants.Skip(1))
                    {
                        Instances.GameplayActivity.Board.SeedBanks.OpponentItem().AddSeed(seedType, true);
                    }
                }
            }
        }

        // Set fisst turn to Zombie by default
        var seedChooserVSSwap = UnityEngine.Object.FindObjectOfType<SeedChooserVSSwap>();
        seedChooserVSSwap.swapCanvasOrder();
        seedChooserVSSwap.m_vsSeedChooserAnimator.Play(-160334332, 0, 1f);
        seedChooserVSSwap.playerTurn = 1;
        seedChooserVSSwap.GetComponent<VersusChooserSwapBinder>().PlayerTurn = 1;
        DisableSeedPackets();
    }

    // Hide disabled seed packets 
    private static void DisableSeedPackets()
    {
        List<ChosenSeed> chosenSeeds = [
            .. Instances.GameplayActivity.SeedChooserScreen.mChosenSeeds,
            .. Instances.GameplayActivity.SeedChooserScreen.mChosenZombies,
        ];

        foreach (var seedPacket in chosenSeeds)
        {
            if (SeedPacketDefinitions.DisabledSeedTypes.Contains(seedPacket.mSeedType))
            {
                seedPacket.mIsImitater = true;
            }
        }
    }
}

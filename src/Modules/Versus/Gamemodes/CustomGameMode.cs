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

        List<ChosenSeed> chosenSeeds = [
            .. Instances.GameplayActivity.SeedChooserScreen.mChosenSeeds,
            .. Instances.GameplayActivity.SeedChooserScreen.mChosenZombies,
        ];

        IArenaSetupSeedbank.AddInitialSeedsToBanks();

        ReplantedClientData.LocalClient?.Ready.Value = true;

        if (ModInfo.DEBUG)
        {
            if (ReplantedLobby.GetLobbyMemberCount() == 1)
            {
                // Set up opponent seed bank for debugging
                var opponentSeedBankInfo = PvZRUtils.GetOpponentSeedBankInfo();
                if (VersusState.AmPlantSide)
                {
                    SeedType[] quickPlayZombies = IArenaSetupSeedbank.GetQuickPlayZombies();
                    for (int i = opponentSeedBankInfo.mSeedsInBank; i < quickPlayZombies.Length; i++)
                    {
                        SeedType seedType = quickPlayZombies[i];
                        opponentSeedBankInfo.AddSeedFromChooser(seedType);
                    }

                    var seedChooserVSSwapDebug = UnityEngine.Object.FindObjectOfType<SeedChooserVSSwap>();
                    seedChooserVSSwapDebug.playerTurn = 1;
                    seedChooserVSSwapDebug.GetComponent<VersusChooserSwapBinder>().PlayerTurn = 1;
                    DisableSeedPackets(chosenSeeds);
                    Instances.GameplayActivity.VersusMode.Phase = VersusPhase.ChoosePlantPacket;

                    yield break;
                }
                else if (VersusState.AmZombieSide)
                {
                    SeedType[] quickPlayPlants = IArenaSetupSeedbank.GetQuickPlayPlants();
                    for (int i = opponentSeedBankInfo.mSeedsInBank; i < quickPlayPlants.Length; i++)
                    {
                        SeedType seedType = quickPlayPlants[i];
                        opponentSeedBankInfo.AddSeedFromChooser(seedType);
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

        foreach (var seedPacket in chosenSeeds)
        {
            if (!ICharacterConfig.IsAllowedInArena(seedPacket.mSeedType, VersusState.Arena))
            {
                seedPacket.mSeedState = ChosenSeedState.SeedPacketHidden;
            }
        }

        DisableSeedPackets(chosenSeeds);
    }

    // Hide disabled seed packets 
    private static void DisableSeedPackets(List<ChosenSeed> chosenSeeds)
    {
        foreach (var seedPacket in chosenSeeds)
        {
            if (SeedPacketDefinitions.DisabledSeedTypes.Contains(seedPacket.mSeedType))
            {
                seedPacket.mIsImitater = true;
            }
        }
    }
}

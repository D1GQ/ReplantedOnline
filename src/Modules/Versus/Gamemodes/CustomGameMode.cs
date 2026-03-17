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
        NetClient.LocalClient?.Ready = false;
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
            if (!NetLobby.AmInLobby())
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

        // Add custom initial seeds
        if (IArena.GetCurrentArena() is ISetupSeedbank setupSeedbank)
        {
            setupSeedbank.SetupSeedbank(Instances.GameplayActivity.Board.SeedBanks.LocalItem(), Instances.GameplayActivity.SeedChooserScreen.m_seedBankInfos._items[0], NetClient.LocalClient.Team);
            setupSeedbank.SetupSeedbank(Instances.GameplayActivity.Board.SeedBanks.OpponentItem(), Instances.GameplayActivity.SeedChooserScreen.m_seedBankInfos._items[1], NetClient.LocalClient.Team.GetOppositeTeam());
        }

        NetClient.LocalClient?.Ready = true;

        if (ModInfo.DEBUG)
        {
            if (NetLobby.GetLobbyMemberCount() == 1)
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

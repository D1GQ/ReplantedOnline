using Il2Cpp;
using Il2CppReloaded.Gameplay;
using Il2CppSource.Binders;
using Il2CppSource.Utils;
using MelonLoader;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Helper;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Gameplay.UI;
using System.Collections;

namespace ReplantedOnline.Network.ClientRPC;

[RegisterRPCHandler]
internal sealed class StartGameClientRPC : BaseClientRPCHandler
{
    /// <inheritdoc/>
    internal sealed override ClientRpcType Rpc => ClientRpcType.StartGame;

    internal static void Send(SelectionSet selectionSet)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteByte((byte)selectionSet);
        NetworkDispatcher.SendRpc(ClientRpcType.StartGame, packetWriter, true);
        packetWriter.Recycle();
        NetLobby.LobbyData.Networked.SetHasStarted(true);
        MatchmakingManager.SetJoinable(false);
    }

    /// <inheritdoc/>
    internal sealed override void Handle(SteamNetClient sender, PacketReader packetReader)
    {
        // Only process StartGame RPCs from the actual lobby host
        if (sender.AmHost)
        {
            var selectionSet = (SelectionSet)packetReader.ReadByte();

            MelonLogger.Msg("[RPCHandler] Game Starting...");

            // Configure the game with the host's selected game mode
            Instances.GameplayActivity.VersusMode.SelectionSet = selectionSet;

            switch (selectionSet)
            {
                case SelectionSet.CustomAll:
                    NetLobby.LobbyData.UnsetAllClientsReady();
                    Instances.GameplayActivity.VersusMode.Phase = VersusPhase.ChooseZombiePacket;
                    Transitions.ToChooseSeeds();
                    Instances.GameplayActivity.StartCoroutine(CoWaitSeedChooserVSSwap().WrapToIl2cpp());
                    break;
                case SelectionSet.Random:
                case SelectionSet.QuickPlay:
                    VersusLobbyPatch.VsSideChooser?.gameObject?.SetActive(false);
                    Instances.GameplayActivity.VersusMode.Phase = VersusPhase.Gameplay;
                    StateTransitionUtils.Transition("InGame");
                    break;
            }
        }
        else
        {
            MelonLogger.Warning($"[RPCHandler] Rejected StartGame RPC from non-host: {sender.Name}");
        }
    }

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

        if (!SteamNetClient.LocalClient.Ready)
        {
            SetClientReadyClientRPC.Send();
        }

        if (ModInfo.DEBUG)
        {
            if (NetLobby.GetLobbyMemberCount() == 1)
            {
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
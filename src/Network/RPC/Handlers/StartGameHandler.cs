using Il2CppReloaded.Gameplay;
using Il2CppSource.Utils;
using MelonLoader;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.UI;

namespace ReplantedOnline.Network.RPC.Handlers;

/// <summary>
/// Handles the StartGame RPC for initiating online Versus matches in ReplantedOnline.
/// Responsible for synchronizing game start and seed selection between players.
/// </summary>
[RegisterRPCHandler]
internal sealed class StartGameHandler : RPCHandler
{
    /// <inheritdoc/>
    internal sealed override RpcType Rpc => RpcType.StartGame;

    internal static void Send(SelectionSet selectionSet)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteByte((byte)selectionSet);
        NetworkDispatcher.SendRpc(RpcType.StartGame, packetWriter, true);
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
                    Instances.GameplayActivity.VersusMode.Phase = VersusPhase.ChoosePlantPacket;
                    Transitions.ToChooseSeeds();
                    break;
                case SelectionSet.Random:
                case SelectionSet.QuickPlay:
                    LobbyPatch.VsSideChooser?.gameObject?.SetActive(false);
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
}
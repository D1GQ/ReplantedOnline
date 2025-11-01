using Il2CppReloaded.Gameplay;
using MelonLoader;
using ReplantedOnline.Items.Attributes;
using ReplantedOnline.Items.Enums;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Packet;

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

    /// <summary>
    /// Sends a StartGame RPC to all connected clients to initiate a game session.
    /// </summary>
    /// <param name="selectionSet">The plant selection set to use for the game.</param>
    internal static void Send(SelectionSet selectionSet)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteByte((byte)selectionSet);
        NetworkDispatcher.SendRpc(RpcType.StartGame, packetWriter, true);
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
                    Instances.GameplayActivity.VersusMode.Phase = VersusPhase.Gameplay;
                    Instances.GameplayActivity.StartPlaying();
                    break;
            }
        }
        else
        {
            MelonLogger.Warning($"[RPCHandler] Rejected StartGame RPC from non-host: {sender.Name}");
        }
    }
}
using Il2CppReloaded.Gameplay;
using MelonLoader;
using ReplantedOnline.Items.Enums;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Packet;

namespace ReplantedOnline.Network.Online;

/// <summary>
/// Handles Remote Procedure Calls (RPCs) for ReplantedOnline game events.
/// Provides methods for sending and receiving game state synchronization between clients.
/// </summary>
internal static class RPC
{
    /// <summary>
    /// Sends an RPC to start the game with the specified plant selection set.
    /// </summary>
    /// <param name="selectionSet">The plant selection set to use for the game.</param>
    internal static void SendStartGame(SelectionSet selectionSet)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteByte((byte)selectionSet);
        NetworkDispatcher.SendRpc(RpcType.StartGame, packetWriter, true);
    }

    /// <summary>
    /// Handles an incoming game start RPC from the host.
    /// </summary>
    /// <param name="sender">The client that sent the RPC (should be the host).</param>
    /// <param name="selectionSet">The plant selection set to use for the game.</param>
    internal static void HandleGameStart(SteamNetClient sender, SelectionSet selectionSet)
    {
        if (sender.IsHost)
        {
            MelonLogger.Msg("Game Starting...");
            ReplantAPI.Core.ReplantAPI.GameplayActivity.VersusMode.SelectionSet = selectionSet;
            ReplantAPI.Core.ReplantAPI.GameplayActivity.VersusMode.Phase = VersusPhase.ChoosePlantPacket;
            Transitions.ToVersus();
        }
    }

    /// <summary>
    /// Sends an RPC to update the game state on all clients.
    /// </summary>
    /// <param name="gameState">The new game state to synchronize.</param>
    internal static void SendUpdateGameState(GameState gameState)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteByte((byte)gameState);
        NetworkDispatcher.SendRpc(RpcType.UpdateGameState, packetWriter, true);
    }

    /// <summary>
    /// Handles an incoming game state update RPC from the host.
    /// </summary>
    /// <param name="sender">The client that sent the RPC (should be the host).</param>
    /// <param name="gameState">The new game state to apply.</param>
    internal static void HandleUpdateGameState(SteamNetClient sender, GameState gameState)
    {
        if (sender.IsHost)
        {
            MelonLogger.Msg($"Updating GameState: {gameState}");

            switch (gameState)
            {
                case GameState.Lobby:
                    break;
                case GameState.PlantChoosingSeed:
                    ReplantAPI.Core.ReplantAPI.GameplayActivity.VersusMode.Phase = VersusPhase.ChoosePlantPacket;
                    break;
                case GameState.ZombieChoosingSeed:
                    ReplantAPI.Core.ReplantAPI.GameplayActivity.VersusMode.Phase = VersusPhase.ChooseZombiePacket;
                    break;
                case GameState.Gameplay:
                    ReplantAPI.Core.ReplantAPI.GameplayActivity.VersusMode.Phase = VersusPhase.Gameplay;
                    Transitions.ToGameplay();
                    break;
            }
        }
    }
}
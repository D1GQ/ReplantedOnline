using Il2CppReloaded.Gameplay;
using MelonLoader;
using ReplantedOnline.Items.Attributes;
using ReplantedOnline.Items.Enums;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Packet;

namespace ReplantedOnline.Network.RPC.Handlers;

/// <summary>
/// Handles the UpdateGameState RPC for synchronizing game phase transitions in ReplantedOnline.
/// Manages the progression of game states from lobby to gameplay and all intermediate phases.
/// </summary>
/// <remarks>
/// This handler ensures all clients remain in sync with the current game phase,
/// providing a consistent multiplayer experience throughout the match lifecycle.
/// </remarks>
[RegisterRPCHandler]
internal sealed class UpdateGameStateHandler : RPCHandler
{
    /// <inheritdoc/>
    internal sealed override RpcType Rpc => RpcType.UpdateGameState;

    /// <summary>
    /// Sends an UpdateGameState RPC to synchronize game phase across all connected clients.
    /// </summary>
    /// <param name="gameState">The new game state to transition to.</param>
    /// <param name="updateLocally">Whether the local client should also process this state change.</param>
    internal static void Send(GameState gameState, bool updateLocally = true)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteByte((byte)gameState);
        NetworkDispatcher.SendRpc(RpcType.UpdateGameState, packetWriter, updateLocally);
    }

    /// <inheritdoc/>
    internal sealed override void Handle(SteamNetClient sender, PacketReader packetReader)
    {
        var gameState = (GameState)packetReader.ReadByte();

        // Only process state updates from the actual lobby host
        if (sender.AmHost)
        {
            MelonLogger.Msg($"[RPCHandler] Updating GameState: {gameState}");

            switch (gameState)
            {
                case GameState.Lobby:
                    // No action needed for lobby state - players are already in lobby
                    break;
                case GameState.HostChoosePlants:
                    // Placeholder for future host-specific plant selection phase
                    break;
                case GameState.HostChooseZombie:
                    // Placeholder for future host-specific zombie selection phase  
                    break;
                case GameState.PlantChoosingSeed:
                    // Transition to plant seed selection phase
                    Instances.GameplayActivity.VersusMode.Phase = VersusPhase.ChoosePlantPacket;
                    break;
                case GameState.ZombieChoosingSeed:
                    // Transition to zombie seed selection phase
                    Instances.GameplayActivity.VersusMode.Phase = VersusPhase.ChooseZombiePacket;
                    break;
                case GameState.Gameplay:
                    // Transition to active gameplay phase
                    Instances.GameplayActivity.VersusMode.Phase = VersusPhase.Gameplay;
                    Transitions.ToGameplay();
                    break;
                default:
                    MelonLogger.Warning($"[RPCHandler] Unknown game state: {gameState}");
                    break;
            }
        }
        else
        {
            MelonLogger.Warning($"[RPCHandler] Rejected GameState update from non-host: {sender.Name}");
        }

        // Always update the last known game state for synchronization purposes
        NetLobby.LobbyData.LastGameState = gameState;
    }
}
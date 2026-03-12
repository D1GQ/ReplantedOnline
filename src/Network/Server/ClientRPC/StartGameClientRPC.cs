using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Server.Packet;

namespace ReplantedOnline.Network.Server.ClientRPC;

[RegisterClientRPC]
internal sealed class StartGameClientRPC : BaseClientRPC
{
    /// <inheritdoc/>
    internal sealed override ClientRpcType Rpc => ClientRpcType.StartGame;

    internal static void Send(SelectionSet selectionSet)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteByte((byte)selectionSet);
        NetworkDispatcher.SendRpc(ClientRpcType.StartGame, packetWriter, true);
        packetWriter.Recycle();
        NetLobby.LobbyData.HasStarted = true;
        MatchmakingManager.UpdateLobbyJoinable();
    }

    /// <inheritdoc/>
    internal sealed override void Handle(NetClient sender, PacketReader packetReader)
    {
        // Only process StartGame RPCs from the actual lobby host
        if (sender.AmHost)
        {
            var selectionSet = (SelectionSet)packetReader.ReadByte();

            ReplantedOnlineMod.Logger.Msg("[RPCHandler] Game Starting...");

            // Configure the game with the host's selected game mode
            Instances.GameplayActivity.VersusMode.SelectionSet = selectionSet;
            var gamemode = VersusGameplayManager.SetGamemode(selectionSet);
            gamemode.OnGameModeStart(Instances.GameplayActivity.VersusMode);
        }
        else
        {
            ReplantedOnlineMod.Logger.Warning($"[RPCHandler] Rejected StartGame RPC from non-host: {sender.Name}");
        }
    }
}
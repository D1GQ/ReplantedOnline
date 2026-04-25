using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Network.Routing;
using ReplantedOnline.Patches.Gameplay.UI;

namespace ReplantedOnline.Network.Client.Rpc;

[RegisterRpc]
internal sealed class StartGameRpc : IRpcDispatcher<SelectionSet>
{
    /// <inheritdoc/>
    public RpcType Rpc => RpcType.StartGame;

    /// <inheritdoc/>
    public void Send(SelectionSet selectionSet)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteByte((byte)selectionSet);
        NetworkDispatcher.SendRpc(Rpc, packetWriter, true);
        packetWriter.Recycle();
        ReplantedLobby.LobbyData.Synced_HasStarted = true;
        MatchmakingManager.UpdateLobbyJoinable();
    }

    /// <inheritdoc/>
    public void Handle(ReplantedClientData sender, PacketReader packetReader)
    {
        // Only process StartGame RPCs from the actual lobby host
        if (sender.AmHost)
        {
            var selectionSet = (SelectionSet)packetReader.ReadByte();

            ReplantedOnlineMod.Logger.Msg("[RPCHandler] Game Starting...");

            // Configure the game with the host's selected game mode
            LevelEntries.SetupVersusArenaForGameplay(selectionSet);
            Instances.GameplayActivity.VersusMode.SelectionSet = selectionSet;
            IVersusGamemode.GetCurrentGamemode()?.OnGameModeStart(Instances.GameplayActivity.VersusMode);
            VersusLobbyPatch.HideLobbyBackground();
        }
        else
        {
            ReplantedOnlineMod.Logger.Warning($"[RPCHandler] Rejected StartGame RPC from non-host: {sender.Name}");
        }
    }
}
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
using ReplantedOnline.Utilities;

namespace ReplantedOnline.Network.Client.Rpc;

[RegisterRpc(RpcType.StartGame)]
internal sealed class StartGameRpc : IRpcDispatcher<SelectionSet>
{
    /// <inheritdoc/>
    public void Send(SelectionSet selectionSet)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteEnum(selectionSet);
        NetworkDispatcher.SendRpc(RpcType.StartGame, packetWriter, true);
        packetWriter.Recycle();
        ReplantedLobby.LobbyData.HasStarted.Value = true;
        MatchmakingManager.UpdateLobbyJoinable();
    }

    /// <inheritdoc/>
    public void Handle(ReplantedClientData sender, PacketReader packetReader)
    {
        // Only process StartGame RPCs from the actual lobby host
        if (sender.AmHost)
        {
            var selectionSet = packetReader.ReadEnum<SelectionSet>();

            ReplantedOnlineMod.Logger.Msg(typeof(StartGameRpc), "Game Starting...");

            // Configure the game with the host's selected game mode
            LevelEntries.SetupVersusArenaForGameplay(selectionSet);
            Instances.GameplayActivity.VersusMode.SelectionSet = selectionSet;
            IVersusGamemode.GetCurrentGamemode()?.OnGameModeStart(Instances.GameplayActivity.VersusMode);
            VersusLobbyPatch.HideLobbyBackground();
        }
        else
        {
            ReplantedOnlineMod.Logger.Warning(typeof(StartGameRpc), $"Rejected StartGame RPC from non-host: {sender.Name}");
        }
    }
}
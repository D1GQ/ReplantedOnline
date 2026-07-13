using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Network;
using ReplantedOnline.Interfaces.Network;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Managers.Reloaded;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Modules.Reloaded;
using ReplantedOnline.Modules.Reloaded.Versus.Gamemodes;
using ReplantedOnline.Network.Reloaded.Client.Routing.Packet;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Patches.Reloaded.Gameplay.UI;
using ReplantedOnline.Utilities.MelonLoader;

namespace ReplantedOnline.Network.Reloaded.Client.Routing.Rpc;

[RegisterRpc(RpcType.StartGame)]
internal sealed class StartGameRpc : IRpcMessage<SelectionSet>
{
    /// <inheritdoc/>
    public void Send(SelectionSet selectionSet)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteEnum(selectionSet);
        if (selectionSet == SelectionSet.Random)
        {
            var zombieSeedTypes = RandomGamemode.PickZombieSeedPacketTypes();
            var plantSeedTypes = RandomGamemode.PickPlantSeedPacketTypes(zombieSeedTypes.AsReadOnly());
            packetWriter.WritePackedInt(zombieSeedTypes.Count);
            foreach (var zombieSeedType in zombieSeedTypes)
            {
                packetWriter.WriteEnum(zombieSeedType);
            }
            packetWriter.WritePackedInt(plantSeedTypes.Count);
            foreach (var plantSeedType in plantSeedTypes)
            {
                packetWriter.WriteEnum(plantSeedType);
            }
        }
        NetworkManager.Packet<RpcPacket>.Singleton.Send(RpcType.StartGame, packetWriter, true);
        packetWriter.Recycle();
        ReloadedLobby.LobbyData!.HasStarted.Value = true;
        ReloadedMatchmaking.UpdateLobbyJoinable();
    }

    /// <inheritdoc/>
    public void Receive(ReloadedClientData sender, PacketReader packetReader)
    {
        // Only process StartGame RPCs from the actual lobby host
        if (sender.AmHost)
        {
            var selectionSet = packetReader.ReadEnum<SelectionSet>();
            if (selectionSet == SelectionSet.Random)
            {
                RandomGamemode.ChosenZombiesSeedTypes.Clear();
                RandomGamemode.ChosenPlantSeedTypes.Clear();
                int zombieSeedTypesCount = packetReader.ReadPackedInt();
                for (int i = 0; i < zombieSeedTypesCount; i++)
                {
                    SeedType zombieSeedType = packetReader.ReadEnum<SeedType>();
                    RandomGamemode.ChosenZombiesSeedTypes.Add(zombieSeedType);
                }
                int plantSeedTypesCount = packetReader.ReadPackedInt();
                for (int i = 0; i < plantSeedTypesCount; i++)
                {
                    SeedType plantSeedType = packetReader.ReadEnum<SeedType>();
                    RandomGamemode.ChosenPlantSeedTypes.Add(plantSeedType);
                }
            }

            ReplantedOnlineMod.Logger.Msg(typeof(StartGameRpc), "Game Starting...");

            // Configure the game with the host's selected game mode
            LevelEntries.SetupVersusArenaForGameplay(selectionSet);
            Instances.GameplayActivity.VersusMode.SelectionSet = selectionSet;
            IVersusGamemode.GetCurrentGamemode()?.OnGameModeStart(Instances.GameplayActivity.VersusMode);
            VersusLobbyPatch.HideLobbyBackground();
            InputManager.SetListeningForNewDevice(false);
        }
        else
        {
            ReplantedOnlineMod.Logger.Warning(typeof(StartGameRpc), $"Rejected StartGame RPC from non-host: {sender.Name}");
        }
    }
}
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Items.Attributes;
using ReplantedOnline.Items.Enums;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Versus.NetworkSync;

namespace ReplantedOnline.Network.RPC.Handlers;

/// <summary>
/// Handles the StartGame RPC for initiating online Versus matches in ReplantedOnline.
/// Responsible for synchronizing game start and seed selection between players.
/// </summary>
[RegisterRPCHandler]
internal sealed class MowZombieHandler : RPCHandler
{
    /// <inheritdoc/>
    internal sealed override RpcType Rpc => RpcType.MowZombie;

    internal static void Send(int row, ZombieNetworked netZombie)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteInt(row);
        packetWriter.WriteNetworkClass(netZombie);
        NetworkDispatcher.SendRpc(RpcType.MowZombie, packetWriter);
    }

    /// <inheritdoc/>
    internal sealed override void Handle(SteamNetClient sender, PacketReader packetReader)
    {
        if (sender.AmPlantSide())
        {
            var row = packetReader.ReadInt();
            var netZombie = (ZombieNetworked)packetReader.ReadNetworkClass();
            var lawnMower = Instances.GameplayActivity.Board.FindLawnMowerInRow(row);
            if (lawnMower != null && !lawnMower.mDead && lawnMower.mMowerState == LawnMowerState.Ready)
            {
                lawnMower.MowZombieOriginal(netZombie._Zombie);
            }
        }
    }
}
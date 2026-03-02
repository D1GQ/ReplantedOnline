using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Server.Packet;
using ReplantedOnline.Network.Steam;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;

namespace ReplantedOnline.Network.Server.ClientRPC;

[RegisterClientRPC]
internal sealed class MowZombieClientRPC : BaseClientRPC
{
    /// <inheritdoc/>
    internal sealed override ClientRpcType Rpc => ClientRpcType.MowZombie;

    internal static void Send(int row, ZombieNetworked netZombie)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteInt(row);
        packetWriter.WriteNetworkObject(netZombie);
        NetworkDispatcher.SendRpc(ClientRpcType.MowZombie, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    internal sealed override void Handle(SteamNetClient sender, PacketReader packetReader)
    {
        if (sender.Team is PlayerTeam.Plants)
        {
            var row = packetReader.ReadInt();
            var netZombie = packetReader.ReadNetworkObject<ZombieNetworked>();
            var lawnMower = Instances.GameplayActivity.Board.FindLawnMowerInRow(row);
            lawnMower.MowZombieOriginal(netZombie._Zombie);
        }
    }
}
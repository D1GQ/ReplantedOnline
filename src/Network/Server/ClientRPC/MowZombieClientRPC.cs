using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Server.Packet;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;

namespace ReplantedOnline.Network.Server.ClientRPC;

[RegisterClientRPC]
internal sealed class MowZombieClientRPC : BaseClientRPC
{
    /// <inheritdoc/>
    internal sealed override ClientRpcType Rpc => ClientRpcType.MowZombie;

    internal static void Send(LawnMower lawnMower, ZombieNetworked netZombie)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteInt(lawnMower.DataID);
        packetWriter.WriteNetworkObject(netZombie);
        NetworkDispatcher.SendRpc(ClientRpcType.MowZombie, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    internal sealed override void Handle(NetClient sender, PacketReader packetReader)
    {
        if (sender.Team == PlayerTeam.Plants)
        {
            var id = packetReader.ReadInt();
            var netZombie = packetReader.ReadNetworkObject<ZombieNetworked>();
            var lawnMower = Instances.GameplayActivity.Board.m_lawnMowers.DataArrayGet(id);

            if (lawnMower != null)
            {
                lawnMower.MowZombieOriginal(netZombie._Zombie);
            }
        }
    }
}
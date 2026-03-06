using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Server.Packet;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;

namespace ReplantedOnline.Network.Server.ClientRPC;

[RegisterClientRPC]
internal sealed class StartMowerClientRPC : BaseClientRPC
{
    /// <inheritdoc/>
    internal sealed override ClientRpcType Rpc => ClientRpcType.StartMower;

    internal static void Send(LawnMower lawnMower)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteInt(lawnMower.DataID);
        NetworkDispatcher.SendRpc(ClientRpcType.StartMower, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    internal sealed override void Handle(NetClient sender, PacketReader packetReader)
    {
        if (sender.Team == PlayerTeam.Plants)
        {
            var id = packetReader.ReadInt();
            var lawnMower = Instances.GameplayActivity.Board.m_lawnMowers.DataArrayGet(id);

            try
            {
                // Only want to start the mower so give a null ref
                lawnMower?.MowZombieOriginal(null);
            }
            catch { }
        }
    }
}
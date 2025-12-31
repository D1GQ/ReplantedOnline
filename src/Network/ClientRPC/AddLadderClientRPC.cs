using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Network.Packet;
using ReplantedOnline.Patches.Gameplay.Versus.Networked;

namespace ReplantedOnline.Network.ClientRPC;

[RegisterRPCHandler]
internal sealed class AddLadderClientRPC : BaseClientRPCHandler
{
    /// <inheritdoc/>
    internal sealed override ClientRpcType Rpc => ClientRpcType.AddLadder;

    internal static void Send(int theGridX, int theGridY)
    {
        var packetWriter = PacketWriter.Get();
        packetWriter.WriteInt(theGridX);
        packetWriter.WriteInt(theGridY);
        NetworkDispatcher.SendRpc(ClientRpcType.AddLadder, packetWriter);
        packetWriter.Recycle();
    }

    /// <inheritdoc/>
    internal sealed override void Handle(SteamNetClient sender, PacketReader packetReader)
    {
        if (sender.Team is PlayerTeam.Plants)
        {
            int gridX = packetReader.ReadInt();
            int gridY = packetReader.ReadInt();
            Instances.GameplayActivity.Board.AddALadderOriginal(gridX, gridY);
        }
    }
}